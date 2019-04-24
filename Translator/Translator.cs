using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BF
{
    public class Translator
    {
        public int MemorySize { get; internal set; }
        public string OutputDirectory { get; internal set; }

        public Assembly Translate(char[] program)
        {
            return EmitRunable(program);
        }

        public string Translate(char[] program, string outputFileName)
        {
            var outputModuleName = outputFileName + ".exe";
            var outputFullName = Path.Combine(OutputDirectory, outputModuleName);

            var assemblyBuilder = EmitExecutable(program, outputFileName, outputModuleName);

            assemblyBuilder.Save(outputModuleName);

            return outputFullName;
        }

        private AssemblyBuilder EmitRunable(char[] program)
        {
            AppDomain ad = AppDomain.CurrentDomain;

            AssemblyName am = new AssemblyName
            {
                Name = "BFIL"
            };

            AssemblyBuilder assemblyBuilder = ad.DefineDynamicAssembly(am, AssemblyBuilderAccess.Run);

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("BFModule");

            TypeBuilder programTypeBuilder = moduleBuilder.DefineType("BFProgram", TypeAttributes.Public);

            programTypeBuilder.AddInterfaceImplementation(typeof(IBFProgram));

            MethodBuilder executeMethodBuilder = programTypeBuilder.DefineMethod(
                "Execute", MethodAttributes.Public | MethodAttributes.Virtual, null, null);

            ILGenerator il = executeMethodBuilder.GetILGenerator();

            Generate(il, program);

            var interfaceExecuteMethodInfo = typeof(IBFProgram).GetMethod(nameof(IBFProgram.Execute));

            programTypeBuilder.DefineMethodOverride(executeMethodBuilder, interfaceExecuteMethodInfo);

            programTypeBuilder.CreateType();

            return assemblyBuilder;
        }

        private AssemblyBuilder EmitExecutable(char[] program, string outputFileName, string moduleFileName)
        {
            AppDomain ad = AppDomain.CurrentDomain;

            AssemblyName am = new AssemblyName
            {
                Name = outputFileName
            };

            AssemblyBuilder assemblyBuilder = ad.DefineDynamicAssembly(am, AssemblyBuilderAccess.Save);

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("BFModule", moduleFileName);

            TypeBuilder programTypeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Public);

            MethodBuilder mainMethodBuilder = programTypeBuilder.DefineMethod(
                "Main",
                MethodAttributes.Public | MethodAttributes.Static,
                null, null);

            assemblyBuilder.SetEntryPoint(mainMethodBuilder);

            ILGenerator il = mainMethodBuilder.GetILGenerator();

            Generate(il, program);

            programTypeBuilder.CreateType();

            return assemblyBuilder;
        }

        private void Generate(ILGenerator il, char[] program)
        {
            var argsInt = new Type[] { typeof(int) };
            var argsChar = new Type[] { typeof(char) };
            var argsBool = new Type[] { typeof(bool) };
            JumpMarker jmpMarker;

            /* Generate jump table */
            var jumpTable = GenerateJumpTable(il, program);

            /* Define labels */
            var lblExit = il.DefineLabel();

            /* Declare locals */
            il.DeclareLocal(typeof(int[])); // [0]: Memory
            il.DeclareLocal(typeof(int));   // [1]: Memory pointer
            il.DeclareLocal(typeof(int));   // [2]: Buffer
            il.DeclareLocal(typeof(char));  // [3]: Inputed char buffer
            il.DeclareLocal(typeof(ConsoleKeyInfo));  // [4]: ConsoleKeyInfo buffer

            /* Create memory */
            il.Emit(OpCodes.Ldc_I4, MemorySize); // Set array size
            il.Emit(OpCodes.Newarr, typeof(int)); // Create array
            il.Emit(OpCodes.Stloc_0); // Save pointer to array to locals[0] (memory)

            /* Translate BF-code */
            // (no optimisation yet!)
            for (var ip = 0; ip < program.Length; ip++)
            {
                switch (program[ip])
                {
                    case '>':
                        il.Emit(OpCodes.Ldloc_1); // put Memory pointer
                        il.Emit(OpCodes.Ldc_I4_1); // put increment
                        il.Emit(OpCodes.Add); // add increment to pointer
                        il.Emit(OpCodes.Stloc_1); // set local (Memory pointer)
                        break;

                    case '<':
                        il.Emit(OpCodes.Ldloc_1); // put Memory pointer
                        il.Emit(OpCodes.Ldc_I4_1); // put decrement
                        il.Emit(OpCodes.Sub); // substract decrement from pointer
                        il.Emit(OpCodes.Stloc_1); // set local (Memory pointer)
                        break;

                    case '+':
                        /* Get value */
                        il.Emit(OpCodes.Ldloc_0); // put Memory array
                        il.Emit(OpCodes.Ldloc_1); // put Memory pointer
                        il.Emit(OpCodes.Ldelem_I4); // get element

                        /* Increment value */
                        // memory value is on top of a stack already
                        il.Emit(OpCodes.Ldc_I4_1); // put increment
                        il.Emit(OpCodes.Add); // add increment to value
                        il.Emit(OpCodes.Stloc_2); // set local (Buffer)

                        /* Set value */
                        il.Emit(OpCodes.Ldloc_0); // put Memory array
                        il.Emit(OpCodes.Ldloc_1); // put Memory pointer
                        il.Emit(OpCodes.Ldloc_2); // put value
                        il.Emit(OpCodes.Stelem_I); // set element
                        break;

                    case '-':
                        /* Get value */
                        il.Emit(OpCodes.Ldloc_0); // put Memory array
                        il.Emit(OpCodes.Ldloc_1); // put Memory pointer
                        il.Emit(OpCodes.Ldelem_I4); // get element

                        /* Increment value */
                        // memory value is on top of a stack already
                        il.Emit(OpCodes.Ldc_I4_1); // put decrement
                        il.Emit(OpCodes.Sub); // substract decrement from value
                        il.Emit(OpCodes.Stloc_2); // set local (Buffer)

                        /* Set value */
                        il.Emit(OpCodes.Ldloc_0); // put Memory array
                        il.Emit(OpCodes.Ldloc_1); // put Memory pointer
                        il.Emit(OpCodes.Ldloc_2); // put value
                        il.Emit(OpCodes.Stelem_I); // set element
                        break;

                    case '.':
                        /* Get value */
                        il.Emit(OpCodes.Ldloc_0); // put Memory array
                        il.Emit(OpCodes.Ldloc_1); // put Memory pointer
                        il.Emit(OpCodes.Ldelem_I4); // get element

                        // Convert int value to char
                        il.EmitCall(
                            OpCodes.Call,
                            typeof(Convert).GetMethod(nameof(Convert.ToChar), argsInt),
                            argsInt);

                        // Write(char)
                        il.EmitCall(
                            OpCodes.Call,
                            typeof(Console).GetMethod(nameof(Console.Write), argsChar),
                            argsChar);

                        break;

                    case ',':

                        // Read key
                        il.EmitCall(
                            OpCodes.Call,
                            typeof(Console).GetMethod(nameof(Console.ReadKey), new Type[] { }),
                            null);

                        il.Emit(OpCodes.Stloc, 4);

                        // Get key char
                        il.Emit(OpCodes.Ldloca, 4);

                        il.EmitCall(
                            OpCodes.Call,
                            typeof(ConsoleKeyInfo).GetProperty(nameof(ConsoleKeyInfo.KeyChar)).GetMethod,
                            null);

                        // Save key char
                        il.Emit(OpCodes.Stloc_3); // Set local (Inputed char buffer)

                        // Check if key char is Escape
                        il.Emit(OpCodes.Ldloc_3); // Put local (Inputed char buffer)
                        il.Emit(OpCodes.Ldc_I4, (int)ConsoleKey.Escape);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Brtrue, lblExit);

                        // Set memory value
                        il.Emit(OpCodes.Ldloc_0); // put Memory array
                        il.Emit(OpCodes.Ldloc_1); // put Memory pointer
                        il.Emit(OpCodes.Ldloc_3); // Put local (Inputed char buffer)
                        il.Emit(OpCodes.Stelem_I); // set element

                        break;

                    case '[':

                        jmpMarker = jumpTable[ip];

                        il.MarkLabel(jmpMarker.CurrentPosLabel);

                        /* Get value */
                        il.Emit(OpCodes.Ldloc_0); // put Memory array
                        il.Emit(OpCodes.Ldloc_1); // put Memory pointer
                        il.Emit(OpCodes.Ldelem_I4); // get element
                        /* Put 0 to compare */
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Brtrue, jumpTable[jmpMarker.TrasitionPosId].CurrentPosLabel);

                        break;

                    case ']':

                        jmpMarker = jumpTable[ip];

                        il.Emit(OpCodes.Br, jumpTable[jmpMarker.TrasitionPosId].CurrentPosLabel);

                        il.MarkLabel(jmpMarker.CurrentPosLabel);

                        break;
                }
            }

            /* Return code */
            il.MarkLabel(lblExit);
            il.Emit(OpCodes.Ret);
        }

        private Dictionary<int, JumpMarker> GenerateJumpTable(ILGenerator il, char[] program)
        {
            var result = new Dictionary<int, JumpMarker>();
            var bracketCounter = 0;
            var fluentIp = 0;

            for (var ip = 0; ip < program.Length; ip++)
            {
                switch (program[ip])
                {
                    case '[':
                        bracketCounter = 1;

                        fluentIp = ip;

                        // searching closing bracket
                        while (bracketCounter > 0)
                        {
                            fluentIp++;

                            if (program[fluentIp] == '[')
                            {
                                bracketCounter++;
                            }

                            if (program[fluentIp] == ']')
                            {
                                bracketCounter--;
                            }
                        }

                        result.Add(ip, new JumpMarker
                        {
                            CurrentPosLabel = il.DefineLabel(),
                            TrasitionPosId = fluentIp
                        });

                        break;

                    case ']':
                        bracketCounter = 1;

                        fluentIp = ip;

                        while (bracketCounter > 0)
                        {
                            fluentIp--;

                            if (program[fluentIp] == ']')
                            {
                                bracketCounter++;
                            }

                            if (program[fluentIp] == '[')
                            {
                                bracketCounter--;
                            }
                        }

                        result.Add(ip, new JumpMarker
                        {
                            CurrentPosLabel = il.DefineLabel(),
                            TrasitionPosId = fluentIp
                        });

                        break;
                }
            }

            return result;
        }
    }
}
