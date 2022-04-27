using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaObfuscator
{
    public class Settings
    {
        public Dictionary<string, Settings> Functions { get; set; }
        public Dictionary<string, Settings> Variables { get; set; }
        public int[] MutateAllLiterals { get; set; }
    }
}
