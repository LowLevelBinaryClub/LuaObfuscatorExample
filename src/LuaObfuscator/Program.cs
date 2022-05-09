using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace LuaObfuscator
{
    class Program
    {
        public static void Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Usage: LuaObfuscator script.lua settings.json");
                // Make some demo_settings.json as we have no interface yet

                Settings demoSettings = new Settings()
                {
                    Functions = new Dictionary<string, Settings>()
                    {
                        {"ObfuscateMeLow", new Settings() { MutateAllLiterals = new int[] { 30 } } },
                        {"ObfuscateMeMedium", new Settings() { MutateAllLiterals = new int[] { 60, 10 } } },
                        {"ObfuscateMeHard", new Settings() { MutateAllLiterals = new int[] { 80, 33, 80 } } },
                    },
                };

                #region DemoScript
                string demoScript = @"function NoObfuscate()
    local a = 123
    local b = 901
    local c = 420

    if a > b then
        c = (a/2)+2
    else
        c = (b-a)*8
    end
    return a, b, c
end

function ObfuscateMeLow()
    local a = 123
    local b = 901
    local c = 420

    if a > b then
        c = (a/2)+2
    else
        c = (b-a)*8
    end
    return a, b, c
end

function ObfuscateMeMedium()
    local a = 123
    local b = 901
    local c = 420

    if a > b then
        c = (a/2)+2
    else
        c = (b-a)*8
    end
    return a, b, c
end

function ObfuscateMeHard()
    local a = 123
    local b = 901
    local c = 420

    if a > b then
        c = (a/2)+2
    else
        c = (b-a)*8
    end
    return a, b, c
end
";
                #endregion

                File.WriteAllText("demo_file.lua", demoScript);
                File.WriteAllText("demo_settings.json", JsonConvert.SerializeObject(demoSettings));
                return;
            }

            string filePath = args[0];
            string settingsPath = args[1];

            if(!File.Exists(filePath))
            {
                Console.Write($"Error, invalid file path! ({filePath})");
                return;
            }
            
            if (!File.Exists(settingsPath))
            {
                Console.Write($"Error, invalid settings path! ({settingsPath})");
                return;
            }

            string serializedSettings = File.ReadAllText(settingsPath);
            Settings settings = JsonConvert.DeserializeObject<Settings>(serializedSettings);

            string script = File.ReadAllText(filePath);
            Obfuscator obfuscator = new Obfuscator(script);

            Console.WriteLine("Obfuscating...");
            string result = obfuscator.Obfuscate(settings);

            //Console.WriteLine(new string('=', Console.WindowWidth));
            Console.WriteLine(result);
            //Console.WriteLine(new string('=', Console.WindowWidth));

            File.WriteAllText("out.lua", result);
            Console.WriteLine("Done!");
            return;
        }
    }
}
