using Brainfuck.Core.Syntax;
using System;
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

        public Action Compile(Module module)
        {
            DynamicMethod method = new DynamicMethod(MethodName, null, null);
            ILGenerator il = method.GetILGenerator();
            new Visitor(module, il, Setting).GenerateIL();
            return (Action)method.CreateDelegate(typeof(Action));
        }

        public void CompileToIL(Module module, ILGenerator il)
        {
            new Visitor(module, il, Setting).GenerateIL();
        }

        private class Visitor : IVisitor
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

            private static readonly Dictionary<Type, int> SizeTable = new Dictionary<Type, int>()
            {
                [typeof(Byte)] = 1,
                [typeof(Int16)] = 2,
                [typeof(Int32)] = 4,
                [typeof(Int64)] = 8,
            };

            #endregion

            private readonly Module module;
            private readonly ILGenerator il;
            private readonly Setting setting;
            private LocalBuilder buffer;
            private LocalBuilder ptr;
            private LocalBuilder pinned;

            public Visitor(Module module, ILGenerator il, Setting setting)
            {
                this.module = module;
                this.il = il;
                this.setting = setting;
            }

            public void GenerateIL()
            {
                /**
                 * Initialize locals
                 **/

                buffer = il.DeclareLocal(setting.ElementType.MakeArrayType());

                if (setting.UnsafeCode)
                {
                    pinned = il.DeclareLocal(setting.ElementType.MakeByRefType(), true);
                    ptr = il.DeclareLocal(setting.ElementType.MakePointerType());
                }
                else
                {
                    ptr = il.DeclareLocal(typeof(int));
                }

                /**
                 * Generate IL
                 **/

                // Initialize buffer
                il.Emit(OpCodes.Ldc_I4, setting.BufferSize);
                il.Emit(OpCodes.Newarr, setting.ElementType);
                il.Emit(OpCodes.Stloc, buffer);

                if (setting.UnsafeCode)
                {
                    // Initialize raw pointer
                    // fixed (T* ptr = buffer) {
                    il.Emit(OpCodes.Ldloc, buffer);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ldelema, setting.ElementType);
                    il.Emit(OpCodes.Stloc, pinned);
                    il.Emit(OpCodes.Ldloc, pinned);
                    il.Emit(OpCodes.Conv_I);
                    il.Emit(OpCodes.Stloc, ptr);
                }
                else
                {
                    // Initialize ptr
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stloc, ptr);
                }

                module.Root.Accept(this);

                if (setting.UnsafeCode)
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_I);
                    il.Emit(OpCodes.Stloc, ptr);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_U);
                    il.Emit(OpCodes.Stloc, pinned);
                }

                il.Emit(OpCodes.Ret);
            }

            public void Visit(BlockUnitOperation op)
            {
                EmitOperations(op.Operations, op.PtrChange);
            }

            public void Visit(IfTrueUnitOperation op)
            {
                Label end = il.DefineLabel();

                LoadElement(op.Src.Offset);
                il.Emit(OpCodes.Brfalse, end);
                EmitOperations(op.Operations, op.PtrChange);
                il.MarkLabel(end);
            }

            public void Visit(RoopUnitOperation op)
            {
                Label begin = il.DefineLabel(), end = il.DefineLabel();

                LoadElement(op.Src.Offset);
                il.Emit(OpCodes.Brfalse, end);
                il.MarkLabel(begin);
                EmitOperations(op.Operations, op.PtrChange);
                LoadElement(op.Src.Offset);
                il.Emit(OpCodes.Brtrue, begin);
                il.MarkLabel(end);
            }

            private void EmitOperations(ImmutableArray<IOperation> operations, int ptrChange)
            {
                for (int i = 0; i < operations.Length; i++)
                {
                    operations[i].Accept(this);
                }

                if (ptrChange != 0)
                {
                    AddPtr(ptrChange);
                }
            }

            public void Visit(AddPtrOperation op)
            {
                AddPtr(op.Value);
            }

            public void Visit(AssignOperation op)
            {
                if (setting.UnsafeCode)
                {
                    LoadPtrWithOffset(op.Dest.Offset);
                    il.Emit(OpCodes.Ldc_I4, op.Value);
                    Conv_I4_To_Auto();
                    Stind_Auto();
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, buffer);
                    LoadPtrWithOffset(op.Dest.Offset);
                    il.Emit(OpCodes.Ldc_I4, op.Value);
                    Conv_I4_To_Auto();
                    Stelem_Auto();
                }
            }

            public void Visit(AddAssignOperation op)
            {
                LoadElementAddress(op.Dest.Offset);
                il.Emit(OpCodes.Dup);
                Ldind_Auto();
                Ldc_Auto(op.Value);
                il.Emit(OpCodes.Add);
                Conv_I4_To_Down_Auto();
                Stind_Auto();
            }

            public void Visit(MultAddAssignOperation op)
            {
                LoadElementAddress(op.Dest.Offset);
                il.Emit(OpCodes.Dup);
                Ldind_Auto();
                LoadElement(op.Src.Offset);
                Ldc_Auto(op.Value);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Add);
                Conv_I4_To_Down_Auto();
                Stind_Auto();
            }

            public void Visit(PutOperation op)
            {
                LoadElement(op.Src.Offset);
                il.Emit(OpCodes.Conv_U2);
                il.EmitCall(OpCodes.Call, typeof(Console).GetMethod(nameof(Console.Write), new[] { typeof(char) }), null);
            }

            public void Visit(ReadOperation op)
            {
                if (setting.UnsafeCode)
                {
                    LoadPtrWithOffset(op.Dest.Offset);
                    il.EmitCall(OpCodes.Call, typeof(Console).GetMethod(nameof(Console.Read), new Type[0]), null);
                    Stind_Auto();
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, buffer);
                    LoadPtrWithOffset(op.Dest.Offset);
                    il.EmitCall(OpCodes.Call, typeof(Console).GetMethod(nameof(Console.Read), new Type[0]), null);
                    Stelem_Auto();
                }
            }

            #region Helpers

            private void AddPtr(int diff)
            {
                if (setting.UnsafeCode)
                {
                    il.Emit(OpCodes.Ldloc, ptr);
                    il.Emit(OpCodes.Ldc_I4, diff);
                    il.Emit(OpCodes.Conv_I);
                    il.Emit(OpCodes.Ldc_I4, SizeOfElement);
                    il.Emit(OpCodes.Mul);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Stloc, ptr);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, ptr);
                    il.Emit(OpCodes.Ldc_I4, diff);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Stloc, ptr);
                }
            }

            private void LoadElementAddress(int? offset)
            {
                if (setting.UnsafeCode)
                {
                    LoadPtrWithOffset(offset);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, buffer);
                    il.Emit(OpCodes.Ldloc, ptr);
                    if (offset is int val && val != 0)
                    {
                        il.Emit(OpCodes.Ldc_I4, val);
                        il.Emit(OpCodes.Add);
                    }
                    il.Emit(OpCodes.Ldelema, setting.ElementType);
                }
            }

            private void LoadElement(int? offset)
            {
                if (setting.UnsafeCode)
                {
                    LoadPtrWithOffset(offset);
                    Ldind_Auto();
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, buffer);
                    LoadPtrWithOffset(offset);
                    Ldelem_Auto();
                }
            }

            private void LoadPtrWithOffset(int? offset)
            {
                if (setting.UnsafeCode)
                {
                    il.Emit(OpCodes.Ldloc, ptr);
                    if (offset is int val && val != 0)
                    {
                        il.Emit(OpCodes.Ldc_I4, val);
                        il.Emit(OpCodes.Conv_I);
                        il.Emit(OpCodes.Ldc_I4, SizeOfElement);
                        il.Emit(OpCodes.Mul);
                        il.Emit(OpCodes.Add);
                    }
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, ptr);
                    if (offset is int val && val != 0)
                    {
                        il.Emit(OpCodes.Ldc_I4, val);
                        il.Emit(OpCodes.Add);
                    }
                }
            }

            private void Ldc_Auto(int value)
            {
                if (setting.ElementType == typeof(Byte))
                {
                    il.Emit(OpCodes.Ldc_I4, value);
                    Conv_I4_To_Auto();
                }
                else if (setting.ElementType == typeof(Int16))
                {
                    il.Emit(OpCodes.Ldc_I4, value);
                    Conv_I4_To_Auto();
                }
                else if (setting.ElementType == typeof(Int32))
                {
                    il.Emit(OpCodes.Ldc_I4, value);
                }
                else if (setting.ElementType == typeof(Int64))
                {
                    il.Emit(OpCodes.Ldc_I8, (long)value);
                }
            }

            private void Ldind_Auto() => il.Emit(LdindTable[setting.ElementType]);
            private void Stind_Auto() => il.Emit(StindTable[setting.ElementType]);
            private void Ldelem_Auto() => il.Emit(LdelemTable[setting.ElementType]);
            private void Stelem_Auto() => il.Emit(StelemTable[setting.ElementType]);

            private void Conv_I4_To_Down_Auto()
            {
                if (setting.ElementType == typeof(Byte) || setting.ElementType == typeof(Int16))
                {
                    il.Emit(ConvTable[setting.ElementType]);
                }
            }

            private void Conv_I4_To_Auto()
            {
                il.Emit(ConvTable[setting.ElementType]);
            }

            private int SizeOfElement => SizeTable[setting.ElementType];

            #endregion
        }
    }
}
