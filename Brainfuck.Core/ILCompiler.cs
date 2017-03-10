﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Brainfuck.Core
{
    public sealed class ILCompiler
    {
        #region Constants

        private const string MethodName = "Method";

        #endregion

        public Setting Setting { get; }

        public ILCompiler(Setting setting)
        {
            Setting = setting;
        }

        public Action Compile(Program program)
        {
            DynamicMethod method = new DynamicMethod(MethodName, null, null);
            ILGenerator il = method.GetILGenerator();
            new ILCompilerImplement(program, il, Setting).GenerateIL();
            return (Action)method.CreateDelegate(typeof(Action));
        }
    }

    internal struct ILCompilerImplement
    {
        #region Constants

        private static readonly Dictionary<Type, OpCode> LdindTable = new Dictionary<Type, OpCode>()
        {
            [typeof(Byte)] = OpCodes.Ldind_U1,
            [typeof(Int16)] = OpCodes.Ldind_I2,
            [typeof(Int32)] = OpCodes.Ldind_I4,
            [typeof(Int64)] = OpCodes.Ldind_I8,
        };

        private static readonly Dictionary<Type, OpCode> StindTable = new Dictionary<Type, OpCode>()
        {
            [typeof(Byte)] = OpCodes.Stind_I1,
            [typeof(Int16)] = OpCodes.Stind_I2,
            [typeof(Int32)] = OpCodes.Stind_I4,
            [typeof(Int64)] = OpCodes.Stind_I8,
        };

        private static readonly Dictionary<Type, OpCode> LdelemTable = new Dictionary<Type, OpCode>()
        {
            [typeof(Byte)] = OpCodes.Ldelem_U1,
            [typeof(Int16)] = OpCodes.Ldelem_I2,
            [typeof(Int32)] = OpCodes.Ldelem_I4,
            [typeof(Int64)] = OpCodes.Ldelem_I8,
        };

        private static readonly Dictionary<Type, OpCode> StelemTable = new Dictionary<Type, OpCode>()
        {
            [typeof(Byte)] = OpCodes.Stelem_I1,
            [typeof(Int16)] = OpCodes.Stelem_I2,
            [typeof(Int32)] = OpCodes.Stelem_I4,
            [typeof(Int64)] = OpCodes.Stelem_I8,
        };

        private static readonly Dictionary<Type, OpCode> ConvTable = new Dictionary<Type, OpCode>()
        {
            [typeof(Byte)] = OpCodes.Conv_U1,
            [typeof(Int16)] = OpCodes.Conv_I2,
            [typeof(Int32)] = OpCodes.Conv_I4,
            [typeof(Int64)] = OpCodes.Conv_I8,
        };

        #endregion

        private readonly Program program;
        private readonly ILGenerator il;
        private readonly Setting setting;

        private ImmutableArray<Operation> Operations => program.Operations;

        public ILCompilerImplement(Program program, ILGenerator il, Setting setting)
        {
            this.program = program;
            this.il = il;
            this.setting = setting;
        }

        public void GenerateIL()
        {
            // Labels
            HashSet<int> labeledAddresses = GetLabeledAddresses(program);
            Dictionary<int, Label> labels = new Dictionary<int, Label>();
            foreach (var address in labeledAddresses)
            {
                labels[address] = il.DefineLabel();
            }

            // Variables
            LocalBuilder buffer = il.DeclareLocal(setting.ElementType.MakeArrayType());
            LocalBuilder ptr = il.DeclareLocal(typeof(int));

            /**
             * Generate IL
             **/

            // Initialize buffer
            il.Emit(OpCodes.Ldc_I4, setting.BufferSize);
            il.Emit(OpCodes.Newarr, setting.ElementType);
            il.Emit(OpCodes.Stloc, buffer);

            // Initialize ptr
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, ptr);

            for (int i = 0; i < Operations.Length; i++)
            {
                switch (Operations[i].Opcode)
                {
                    case Opcode.AddPtr:
                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(OpCodes.Ldc_I4, Operations[i].Value);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Stloc, ptr);
                        break;
                    case Opcode.AddValue:
                        il.Emit(OpCodes.Ldloc, buffer);
                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(OpCodes.Ldelema, setting.ElementType);
                        il.Emit(OpCodes.Dup);
                        il.Emit(Ldind_Auto());
                        //
                        il.Emit(OpCodes.Ldc_I4, Operations[i].Value);
                        Conv_I4_To_Upper_Auto();
                        //
                        il.Emit(OpCodes.Add);
                        Conv_I4_To_Down_Auto();
                        il.Emit(Stind_Auto());
                        break;
                    case Opcode.MultAdd:
                        il.Emit(OpCodes.Ldloc, buffer);
                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(OpCodes.Ldc_I4, Operations[i].Value);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Ldelema, setting.ElementType);
                        il.Emit(OpCodes.Dup);
                        il.Emit(Ldind_Auto());
                        //
                        il.Emit(OpCodes.Ldloc, buffer);
                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(Ldelem_Auto());
                        il.Emit(OpCodes.Ldc_I4, Operations[i].Value2);
                        Conv_I4_To_Upper_Auto();
                        il.Emit(OpCodes.Mul);
                        il.Emit(OpCodes.Add);
                        Conv_I4_To_Down_Auto();
                        il.Emit(Stind_Auto());
                        break;
                    case Opcode.Assign:
                        Debug.Assert(Operations[i].Value == 0);
                        il.Emit(OpCodes.Ldloc, buffer);
                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(OpCodes.Ldc_I4, Operations[i].Value2);
                        Conv_I4_To_Auto();
                        il.Emit(Stelem_Auto());
                        break;
                    case Opcode.BrZero:
                    case Opcode.OpeningBracket:
                        il.Emit(OpCodes.Ldloc, buffer);
                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(Ldelem_Auto());
                        il.Emit(OpCodes.Brfalse, labels[Operations[i].Value]);
                        break;
                    case Opcode.ClosingBracket:
                        il.Emit(OpCodes.Ldloc, buffer);
                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(Ldelem_Auto());
                        il.Emit(OpCodes.Brtrue, labels[Operations[i].Value]);
                        break;
                    case Opcode.Put:
                        il.Emit(OpCodes.Ldloc, buffer);
                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(Ldelem_Auto());
                        il.Emit(OpCodes.Conv_U2);
                        il.EmitCall(OpCodes.Call, typeof(Console).GetMethod(nameof(Console.Write), new[] { typeof(char) }), null);
                        break;
                    case Opcode.Read:
                        il.Emit(OpCodes.Ldloc, buffer);
                        il.Emit(OpCodes.Ldloc, ptr);
                        il.EmitCall(OpCodes.Call, typeof(Console).GetMethod(nameof(Console.Read), new Type[0]), null);
                        il.Emit(Stelem_Auto());
                        break;
                    case Opcode.Unknown:
                    default:
                        // Do nothing
                        break;
                }

                // If necessary, we generate label
                if (labeledAddresses.Contains(i))
                {
                    il.MarkLabel(labels[i]);
                }
            }

            il.Emit(OpCodes.Ret);
        }

        private OpCode Ldind_Auto() => LdindTable[setting.ElementType];
        private OpCode Stind_Auto() => StindTable[setting.ElementType];
        private OpCode Ldelem_Auto() => LdelemTable[setting.ElementType];
        private OpCode Stelem_Auto() => StelemTable[setting.ElementType];

        private void Conv_I4_To_Down_Auto()
        {
            if (setting.ElementType == typeof(Byte) || setting.ElementType == typeof(Int16))
            {
                il.Emit(ConvTable[setting.ElementType]);
            }
        }

        private void Conv_I4_To_Upper_Auto()
        {
            if (setting.ElementType == typeof(Int64))
            {
                il.Emit(ConvTable[setting.ElementType]);
            }
        }

        private void Conv_I4_To_Auto()
        {
            il.Emit(ConvTable[setting.ElementType]);
        }

        private HashSet<int> GetLabeledAddresses(Program program)
        {
            var set = new HashSet<int>();

            for (int i = 0; i < program.Operations.Length; i++)
            {
                switch (program.Operations[i].Opcode)
                {
                    case Opcode.BrZero:
                    case Opcode.OpeningBracket:
                    case Opcode.ClosingBracket:
                        set.Add(program.Operations[i].Value);
                        break;
                }
            }

            return set;
        }
    }
}