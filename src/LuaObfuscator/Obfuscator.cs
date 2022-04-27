using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuaScriptToolkit;
using LuaScriptToolkit.LuaNodes;
using LuaScriptToolkit.Parser;

namespace LuaObfuscator
{
    public class Obfuscator
    {
        public string Script;
        private LuaToken[] Tokens = null;
        public LuaNode OldNode;
        public LuaNode NewNode;
        private Random rnd = new Random();
        private int LastNodeId = -1;


        public Obfuscator(string s)
        {
            Script = s;
            Init();
        }

        // init
        //
        public List<string> Init() // init with raw script
        {
            // parse
            Console.WriteLine("Tokenizing...");
            Tokens = LuaTokenizer.Tokenize(Script, true, "", out _);

            return Init(Tokens);
        }
        public List<string> Init(LuaToken[] tokens) // init with tokens
        {
            Console.WriteLine("Parsing...");
            var parser = new LuaParser();
            OldNode = parser.ParseTokens(tokens);
            if (OldNode == null)
                return parser.ErrorMessages;

            return Init(OldNode);
        }
        public List<string> Init(LuaNode node) // init with AST
        {
            // clone nodes
            if (OldNode == null)
                OldNode = node;
            NewNode = node.Clone(); // we be obfuscating this one!
            
            UpdateReferences(NewNode, null);

            return null;
        }


        // The magic sauce we call
        // 
        public string Obfuscate(Settings settings)
        {
            if (settings == null)
                throw new Exception("Invalid Settings");

            DoObfuscation(NewNode, settings);

            return NewNode.WriteLuaScript(true);
        }
        private void DoObfuscation(LuaNode node, Settings settings)
        {
            if (settings == null)
                return;

            // Mutate literals ;D!
            if (settings.MutateAllLiterals != null)
                foreach (var i in settings.MutateAllLiterals)
                    MutateAllLiterals(node, i);

            return;
        }


        // Node helpers
        //
        private void UpdateReferences(LuaNode node, LuaNode parent)
        {
            var next = node.Next();
            node.Parent = parent;
            for (int i = 0; i < next.Length; i++)
            {
                next[i].Parent = node;
                UpdateReferences(next[i], node);
            }
        }
        private void FindAll(LuaNode node, LuaNode.NodeType type, ref List<LuaNode> results)
        {
            if (node.Type == type || type == LuaNode.NodeType.ANY)
                results.Add(node);

            var next = node.Next();
            for (int i = 0; i < next.Length; i++)
                FindAll(next[i], type, ref results);
        }
        private List<LuaNode> RandomSelectNodes(List<LuaNode> targets, int percent)
        {
            if (percent > 100)
                return targets;

            int min = (int)(targets.Count * (percent / 100f));
            int max = (int)((targets.Count * (percent / 100f)) + 1);
            max += (int)(max * 1.05);
            if (max < 0)
                max = 0;

            List<LuaNode> results = new List<LuaNode>();
            while (results.Count < min)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    int c = rnd.Next(0, 100);
                    if (c > percent)
                        continue;

                    results.Add(targets[i]);
                }

                // check if to much
                if (results.Count > max)
                    results.Clear(); // to much, reset
            }
            return results;
        }
        private int GenerateNodeId()
        {
            if (LastNodeId == -1)
            {
                int newHigh = 0;
                findHighestNodeId(NewNode, ref newHigh);
                LastNodeId = newHigh;
                void findHighestNodeId(LuaNode node, ref int id)
                {
                    if (node == null)
                        return;

                    if (id < node.Id)
                        id = node.Id;

                    var next = node.Next();
                    for (int i = 0; i < next.Length; i++)
                        findHighestNodeId(next[i], ref id);
                }
            }
            LastNodeId++;
            return LastNodeId;
        }
        public void WriteNodeToNode(LuaNode node, LuaNode target, LuaNode replace)
        {
            if (node == null)
                return;

            var next = node.Next();
            for (int i = 0; i < next.Length; i++)
                WriteNodeToNode(next[i], target, replace);

            node.Replace(target, replace);
        }

        // Obfuscator interface
        //
        public void MutateAllLiterals(LuaNode node, int percent = 100, bool binaryOnly = false)
        {
            // Example: 2  ->  1 + 1
            //
            List<LuaNode> results = new List<LuaNode>();
            FindAll(node, LuaNode.NodeType.Literal, ref results);

            if (percent != 100)
                results = RandomSelectNodes(results, percent);

            for (int i = 0; i < results.Count; i++)
            {
                if (binaryOnly && results[i].Parent.Type != LuaNode.NodeType.Binary)
                    continue;

                MutateLiteral(results[i]);
                UpdateReferences(node, null);
            }
        }

        // Obfuscator (some actual logic)
        //
        public LuaNode MutateLiteral(LuaNode node)
        {
            NodeLiteral literal = (NodeLiteral)node;
            Random rnd = new Random();

            if (literal.Value == null || literal.Value.Type != LuaType.Number)
                return null;

            NodeBinary mutation = null;
            bool complete = false;
            while (!complete)
            {
                string[] mutateOp = { "+-", "+", "-" }; // TODO: "*", "/" };
                string randomOp = mutateOp[rnd.Next(0, mutateOp.Length)];
                mutation = new NodeBinary(randomOp) { Id = GenerateNodeId() };

                NumberConstant num = (NumberConstant)literal.Value;
                switch (randomOp)
                {
                    case "+-":
                        // mutates twice on low values (2: 123-[1+X]+[123-X])
                        mutation.Operator = "-";
                        NodeBinary secondMutation = new NodeBinary("+") { Id = GenerateNodeId() };
                        double doubleAdd = rnd.Next(10, 2000);
                        double doubleAddSub = rnd.Next(4, (int)doubleAdd); // X
                        mutation.Left = new NodeLiteral(new NumberConstant(doubleAdd + num.Value)) { Id = GenerateNodeId() }; // 1+X
                        mutation.Right = secondMutation;
                        //secondMutation.Left = new NodeLiteral(new NumberConstant(doubleAddSub > num.Value ? doubleAddSub - num.Value : num.Value - doubleAddSub)); // 1-X
                        secondMutation.Left = new NodeLiteral(new NumberConstant(doubleAddSub)) { Id = GenerateNodeId() }; // 1-X
                        secondMutation.Right = new NodeLiteral(new NumberConstant(doubleAddSub > doubleAdd ? doubleAddSub - doubleAdd : doubleAdd - doubleAddSub)) { Id = GenerateNodeId() };
                        complete = true;
                        break;
                    case "+":
                        // split literal
                        double sub = rnd.Next(0, (int)num.Value);
                        NumberConstant subVal = new NumberConstant((double)num.Value - sub);
                        mutation.Left = new NodeLiteral(subVal) { Id = GenerateNodeId() };
                        mutation.Right = new NodeLiteral(new NumberConstant(sub)) { Id = GenerateNodeId() };
                        complete = true;
                        break;
                    case "-":
                        int addMax = (int)num.Value * rnd.Next(2, 5); // add 2-5x bigger nr max
                        int addMin = (int)num.Value * rnd.Next(2, 5) / 10; // 20% ~ 50% min
                        double add = rnd.Next(addMin, (int.MaxValue > addMax ? addMax : int.MaxValue));
                        mutation.Left = new NodeLiteral(new NumberConstant((double)add + num.Value)) { Id = GenerateNodeId() };
                        mutation.Right = new NodeLiteral(new NumberConstant(add)) { Id = GenerateNodeId() };
                        complete = true;
                        break;
                    case "/":
                        // TODO: check if value aint to big?
                        double multiply = (double)rnd.Next(2, 30); // add 2-5x bigger nr max
                        double divvalue = num.Value * (double)multiply;
                        mutation.Left = new NodeLiteral(new NumberConstant((double)multiply)) { Id = GenerateNodeId() };
                        mutation.Right = new NodeLiteral(new NumberConstant((double)divvalue)) { Id = GenerateNodeId() };
                        complete = true;
                        break;
                    default:
                        break;
                }
            }

            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(literal.Parent);
            WriteNodeToNode(NewNode, literal, mutation);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(literal.Parent);
            Console.ForegroundColor = old;
            return mutation;
        }
    }

}
