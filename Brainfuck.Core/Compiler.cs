using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Brainfuck.Core
{
    public sealed class Compiler
    {
        public CompilerSetting Setting { get; }

        public Compiler(CompilerSetting setting)
        {
            Setting = setting;
        }

        public Action Compile(Program program)
        {
            // Variables and local function
            ParameterExpression ptr = Expression.Variable(typeof(int), nameof(ptr));
            ParameterExpression buffer = Expression.Variable(Setting.ElementType.MakeArrayType(), nameof(buffer));
            IndexExpression elem = Expression.ArrayAccess(buffer, ptr);
            ConstantExpression zero = Expression.Constant(ConvertInteger(0, Setting.ElementType), Setting.ElementType);

            var labels = new Dictionary<int, LabelTarget>();
            LabelTarget GetLabel(int index)
            {
                if (labels.TryGetValue(index, out var label))
                {
                    return label;
                }
                else
                {
                    label = Expression.Label();
                    labels.Add(index, label);
                    return label;
                }
            }

            // Generate execution code

            var expressions = new List<Expression>();

            // Initialize variables
            expressions.Add(Expression.Assign(ptr, Expression.Constant(0, typeof(int))));
            expressions.Add(Expression.Assign(buffer, Expression.NewArrayBounds(Setting.ElementType, Expression.Constant(Setting.BufferSize))));

            for (int i = 0; i < program.Operations.Length; i++)
            {
                switch (program.Operations[i].Opcode)
                {
                    case Opcode.AddPtr:
                        expressions.Add(Expression.AddAssign(ptr, Expression.Constant(program.Operations[i].Value)));
                        if (Setting.UseDynamicBuffer && program.Operations[i].Value > 0)
                        {
                            AddCheckBufferCode(expressions, buffer, ptr);
                        }
                        break;
                    case Opcode.AddValue:
                        expressions.Add(Expression.AddAssign(elem, GetConstant(program.Operations[i].Value)));
                        break;
                    case Opcode.Put:
                        expressions.Add(Expression.Call(
                                            typeof(Console).GetRuntimeMethod(nameof(Console.Write), new[] { typeof(char) }),
                                            Expression.Convert(elem, typeof(char))));
                        break;
                    case Opcode.Read:
                        expressions.Add(Expression.Assign(
                                            elem,
                                            Expression.Convert(
                                                Expression.Call(typeof(Console).GetRuntimeMethod(nameof(Console.Read), Array.Empty<Type>())),
                                                Setting.ElementType)));
                        break;
                    case Opcode.OpeningBracket:
                        expressions.Add(Expression.IfThen(
                                            Expression.Equal(elem, zero),
                                            Expression.Goto(GetLabel(program.Operations[i].Value))));
                        expressions.Add(Expression.Label(GetLabel(i)));
                        break;
                    case Opcode.ClosingBracket:
                        expressions.Add(Expression.IfThen(
                                            Expression.NotEqual(elem, zero),
                                            Expression.Goto(GetLabel(program.Operations[i].Value))));
                        expressions.Add(Expression.Label(GetLabel(i)));
                        break;
                    case Opcode.Unknown:
                    default:
                        // Do nothing
                        break;
                }
            }

            Expression block = Expression.Block(new[] { ptr, buffer }, expressions);
            Expression<Action> lambda = Expression.Lambda<Action>(block, Array.Empty<ParameterExpression>());
            return lambda.Compile();
        }

        private ConstantExpression GetConstant(int value) =>
            Expression.Constant(ConvertInteger(value, Setting.ElementType), Setting.ElementType);

        private void AddCheckBufferCode(List<Expression> expressions, ParameterExpression buffer, ParameterExpression ptr)
        {
            Debug.Assert(Setting.UseDynamicBuffer);

            ParameterExpression temp = Expression.Variable(Setting.ElementType.MakeArrayType());
            UnaryExpression lengthOfBuffer = Expression.ArrayLength(buffer);
            BinaryExpression doubleOfCurrentLength = Expression.Multiply(lengthOfBuffer, Expression.Constant(2));
            ConditionalExpression lengthOfNewBuffer = Expression.Condition(
                                                        Expression.GreaterThan(doubleOfCurrentLength, ptr),
                                                        doubleOfCurrentLength, Expression.Increment(ptr));

            expressions.Add(Expression.IfThen(
                                Expression.GreaterThanOrEqual(ptr, lengthOfBuffer),
                                Expression.Block(
                                    new[] { temp },
                                    Expression.Assign(
                                        temp,
                                        Expression.NewArrayBounds(Setting.ElementType, lengthOfNewBuffer)),
                                    Expression.Call(
                                        typeof(Array).GetRuntimeMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(Array), typeof(int) }),
                                        buffer, temp, lengthOfBuffer),
                                    Expression.Assign(
                                        buffer,
                                        temp))));
        }

        private static object ConvertInteger(int value, Type type)
        {
            if (type == typeof(Int16))
            {
                return (Int16)value;
            }
            else if (type == typeof(Int32))
            {
                return value;
            }
            else if (type == typeof(Int64))
            {
                return (Int64)value;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type '{type}'");
            }
        }

        private static void ManageUnknownChar(char value) => Console.WriteLine($"Warning : Unknown char '{value}'");
    }

    public class CompilerSetting
    {
        #region Constants

        internal const int DefaultBufferSize = 1 << 20;
        internal static readonly Type DefaultElementType = typeof(int);
        internal const bool DefaultUseDynamicBuffer = false;

        #endregion

        public int BufferSize { get; }
        public Type ElementType { get; }
        public bool UseDynamicBuffer { get; }

        public CompilerSetting(int bufferSize, Type elementType, bool useDynamicBuffer)
        {
            BufferSize = bufferSize;
            ElementType = elementType;
            UseDynamicBuffer = useDynamicBuffer;
        }

        public static readonly CompilerSetting Default = new CompilerSetting(DefaultBufferSize, DefaultElementType, DefaultUseDynamicBuffer);

        public CompilerSetting WithBufferSize(int bufferSize) => new CompilerSetting(bufferSize, this.ElementType, this.UseDynamicBuffer);
        public CompilerSetting WithElementType(Type elementType) => new CompilerSetting(this.BufferSize, elementType, this.UseDynamicBuffer);
        public CompilerSetting WithUseDynamicBuffer(bool useDynamicBuffer) => new CompilerSetting(this.BufferSize, this.ElementType, useDynamicBuffer);
    }
}
