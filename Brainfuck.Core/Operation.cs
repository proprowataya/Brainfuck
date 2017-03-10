using System;
using System.Collections.Generic;
using System.Text;

namespace Brainfuck.Core
{
    public struct Operation
    {
        public Opcode Opcode { get; }
        public int Value { get; }
        public int Value2 { get; }

        internal Operation(Opcode opcode) : this(opcode, 0)
        { }

        internal Operation(Opcode opcode, int value) : this(opcode, value, 0)
        { }

        internal Operation(Opcode opcode, int value, int value2)
        {
            Opcode = opcode;
            Value = value;
            Value2 = value2;
        }

        public override string ToString() => $"{Opcode}, {Value}, {Value2}";
    }

    public enum Opcode
    {
        AddPtr,
        AddValue,
        Put,
        Read,

        /* Jump */
        BrZero,
        OpeningBracket,
        ClosingBracket,

        MultAdd,
        Assign,
        Unknown,
    }
}
