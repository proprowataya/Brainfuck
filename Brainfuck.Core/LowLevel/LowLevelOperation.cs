using System;

namespace Brainfuck.Core.LowLevel
{
    public struct LowLevelOperation
    {
        public Opcode Opcode { get; }
        public Variable Dest { get; }
        public Variable Src { get; }
        public int Value { get; }

        public LowLevelOperation(Opcode opcode, Variable dest = default(Variable), Variable src = default(Variable), int value = 0)
        {
            Opcode = opcode;
            Dest = dest;
            Src = src;
            Value = value;
        }

        public LowLevelOperation WithDest(Variable newValue) => new LowLevelOperation(Opcode, newValue, Src, Value);
        public LowLevelOperation WithSrc(Variable newValue) => new LowLevelOperation(Opcode, Dest, newValue, Value);

        public override string ToString() => $"{Opcode} {Dest}, {Src}, {Value}";
    }

    public enum Opcode : short
    {
        AddPtr,
        Assign,
        AddAssign,
        MultAddAssign,
        Put,
        Read,
        BrTrue,
        BrFalse,
        Return,
        EnsureBuffer,
    }

    public struct Variable
    {
        private readonly bool _isRegister;
        private readonly int _offset;
        private readonly int _registerNo;

        public bool IsRegister => _isRegister;
        public int Offset => !_isRegister ? _offset : throw new InvalidOperationException();
        public int RegisterNo => _isRegister ? _registerNo : throw new InvalidOperationException();

        private Variable(bool isRegister, int offset = 0, int registerNo = 0)
        {
            _isRegister = isRegister;
            _offset = offset;
            _registerNo = registerNo;
        }

        public static Variable Memory(int offset) => new Variable(false, offset: offset);
        public static Variable Register(int registerNo) => new Variable(true, registerNo: registerNo);

        public override string ToString() => _isRegister ? $"r{_registerNo}" : $"ptr[{_offset}]";
    }
}
