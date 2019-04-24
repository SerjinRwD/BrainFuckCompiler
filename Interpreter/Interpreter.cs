using System;

namespace BF
{
    public class Interpreter
    {
        public int MemorySize { get; }
        public int ProgramBufferSize { get; }
        
        private readonly int[] _memory;
        private readonly char[] _program;

        private int _loadedProgramLength;

        /// <summary>
        /// Memory pointer
        /// </summary>
        private uint _mPtr;

        /// <summary>
        /// Instruction pointer
        /// </summary>
        private uint _iPtr;

        public Interpreter(int memorySize = 30000, int programBufferSize = 50000)
        {
            MemorySize = memorySize;
            ProgramBufferSize = programBufferSize;

            _memory = new int[memorySize];
            _program = new char[programBufferSize];

            _mPtr = _iPtr = 0;

            _loadedProgramLength = 0;
        }

        public void Reset()
        {
            int i;

            for (i = 0; i < MemorySize; i++)
            {
                _memory[i] = 0;
            }

            for (i = 0; i < ProgramBufferSize; i++)
            {
                _program[i] = '\0';
            }

            _mPtr = _iPtr = 0;

            _loadedProgramLength = 0;
        }

        public void Load(char[] program)
        {
            Reset();

            _loadedProgramLength = program.Length;

            Array.Copy(program, _program, _loadedProgramLength);
        }

        public void Execute()
        {
            uint bracketCounter = 0;

            while(_iPtr < _loadedProgramLength)
            {
                switch (_program[_iPtr])
                {
                    case '>':
                        _mPtr++;
                        break;

                    case '<':
                        _mPtr--;
                        break;

                    case '+':
                        _memory[_mPtr]++;
                        break;

                    case '-':
                        _memory[_mPtr]--;
                        break;

                    case '.':
                        Console.Write((char)_memory[_mPtr]);
                        break;

                    case ',':
                        var keyInfo = Console.ReadKey();

                        if (keyInfo.Key == ConsoleKey.Escape)
                        {
                            Console.WriteLine("{0}BF program terminated by user input.", Environment.NewLine);
                            return;
                        }

                        _memory[_mPtr] = keyInfo.KeyChar;
                        break;

                    case '[':
                        if (_memory[_mPtr] == 0)
                        {
                            bracketCounter = 1;

                            while (bracketCounter > 0)
                            {
                                _iPtr++;

                                if (_program[_iPtr] == '[')
                                {
                                    bracketCounter++;
                                }

                                if (_program[_iPtr] == ']')
                                {
                                    bracketCounter--;
                                }
                            }
                        }
                        break;

                    case ']':
                        if (_memory[_mPtr] > 0)
                        {
                            bracketCounter = 1;

                            while (bracketCounter > 0)
                            {
                                _iPtr--;

                                if (_program[_iPtr] == ']')
                                {
                                    bracketCounter++;
                                }

                                if (_program[_iPtr] == '[')
                                {
                                    bracketCounter--;
                                }
                            }
                        }
                        break;
                }

                _iPtr++;
            }
        }
    }
}
