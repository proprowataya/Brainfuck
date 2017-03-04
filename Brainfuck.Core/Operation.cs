using System;
using System.Collections.Generic;
using System.Text;

namespace Brainfuck.Core
{
    public struct Operation
    {
        public Opcode Opcode { get; }
        public int Value { get; }

        internal Operation(Opcode opcode) : this(opcode, 0)
        { }

        internal Operation(Opcode opcode, int value)
        {
            Opcode = opcode;
            Value = value;
        }

        public override string ToString() => $"{Opcode}, {Value}";
    }

    public enum Opcode
    {
        AddPtr,
        AddValue,
        Put,
        Read,
        OpeningBracket,
        ClosingBracket,
        Unknown,
    }
}
