using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace CLI
{
    class Program
    {
        const int InterpreterMode = 0;
        const int TranslatorMode = 1;

        public static string BasePath => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static readonly char[] spaceSplitter = new char[] { ' ' };
        private static int mode = TranslatorMode; // InterpreterMode;

        private readonly static BF.Interpreter bfi;
        private readonly static BF.Translator bft;
        private readonly static Stopwatch sw;
        private static char[] program;

        private static bool terminate;
        private readonly static Dictionary<string, Action<string[]>> commandHandlers;
        private readonly static Dictionary<string, string> commandDescriptions;

        static Program()
        {
            bfi = new BF.Interpreter();
            bft = new BF.TranslatorBuilder()
                .SetOutputDirectory(BasePath)
                .WithMemorySize(30000)
                .Build();

            sw = new Stopwatch();

            terminate = false;

            commandHandlers = new Dictionary<string, Action<string[]>>
            {
                { "!help", WriteHelpHandler },
                { "!quit", QuitHandler },
                { "!clear", ClearConsoleHandler },
                { "!load", LoadProgramHandler },
                { "!compile", CompileProgramHandler },
                { "!mode", ModeHandler },
            };

            commandDescriptions = new Dictionary<string, string>
            {
                { "!help", "this text" },
                { "!quit", "exit" },
                { "!mode", "interpeter (0) or translator (1). If no args: show current mode." },
                { "!load", "read bf-file. Args: [0] - source file path" },
                { "!compile", "translate BF-script to executable file (only in mode 1). Args: [0] - source file path, [1] - output file name" },
                { "!clear", "clear screen" },
            };
        }

        static void Main(string[] args)
        {
            WriteHelpHandler();

            while (true)
            {
                var line = Console.ReadLine();

                if (line.StartsWith("!"))
                {
                    var arr = line.Split(spaceSplitter, StringSplitOptions.RemoveEmptyEntries);

                    HandleInput(arr);

                    if (terminate)
                    {
                        return;
                    }
                }
                else
                {
                    program = line.ToCharArray();
                    ExecuteProgram();
                }
            }
        }

        private static void ExecuteProgram()
        {
            if (program != null && program.Length > 0)
            {
                if (mode == InterpreterMode)
                {
                    bfi.Load(program);

                    sw.Restart();

                    bfi.Execute();

                    sw.Stop();
                }
                else if (mode == TranslatorMode)
                {
                    var asm = bft.Translate(program);

                    var p = (BF.IBFProgram)asm.CreateInstance("BFProgram");

                    sw.Restart();
                    p.Execute();
                    sw.Stop();
                }

                Console.WriteLine();
                Console.WriteLine("Elapsed: {0:d\\.hh\\:mm\\:ss\\.FFFFFF}", sw.Elapsed);
            }

        }

        private static void HandleInput(string[] args)
        {
            if (commandHandlers.ContainsKey(args[0]))
            {
                commandHandlers[args[0]](args);
            }
            else
            {
                Console.WriteLine("Uknown command");
            }
        }

        private static void WriteHelpHandler(string[] args = null)
        {
            Console.WriteLine("Interactive BF. Mode: {0}", mode);
            Console.WriteLine("Type line of BF-script and press Enter");

            foreach(var pair in commandDescriptions)
            {
                Console.WriteLine("{0} - {1}", pair.Key, pair.Value);
            }
        }

        private static void ModeHandler(string[] args)
        {
            if (args.Length == 1)
            {
                Console.WriteLine("Current mode: {0}", mode);
            }
            else if (args.Length > 1 && int.TryParse(args[1], out mode) && mode < 2)
            {
                Console.WriteLine("Set mode: {0}", mode);
            }
            else
            {
                Console.WriteLine("Bad argument");
            }
        }

        private static void CompileProgramHandler(string[] args)
        {
            if (mode != TranslatorMode)
            {
                Console.WriteLine("Only in mode 1 (translator)");
                return;
            }

            var fileExists = true;
            var file = args[1];

            if (!File.Exists(file))
            {
                file = Path.Combine(BasePath, args[1]);

                if (!File.Exists(file))
                {
                    fileExists = false;
                }
            }

            if (fileExists)
            {
                program = File.ReadAllText(file).ToCharArray();

                var output = bft.Translate(program, args[2]);

                Console.WriteLine("Saved to file: \"{0}\"", output);
            }
            else
            {
                Console.WriteLine("File not found: \"{0}\"", args[1]);
            }
        }

        private static void LoadProgramHandler(string[] args)
        {
            var fileExists = true;
            var file = args[1];

            if (!File.Exists(file))
            {
                file = Path.Combine(BasePath, args[1]);

                if (!File.Exists(file))
                {
                    fileExists = false;
                }
            }

            if (fileExists)
            {
                program = File.ReadAllText(file).ToCharArray();
                ExecuteProgram();
            }
            else
            {
                Console.WriteLine("File not found: \"{0}\"", args[1]);
            }
        }

        private static void ClearConsoleHandler(string[] args = null)
        {
            Console.Clear();
        }

        private static void QuitHandler(string[] args = null)
        {
            terminate = true;
            Console.WriteLine("Bye");
        }
    }
}
