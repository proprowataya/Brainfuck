namespace Brainfuck.Core.LowLevel
{
    public struct LowLevelOperation
    {
        public Opcode Opcode { get; }
        public short Dest { get; }
        public short Src { get; }
        public short Value { get; }

        public LowLevelOperation(Opcode opcode, short dest = 0, short src = 0, short value = 0)
        {
            Opcode = opcode;
            Dest = dest;
            Src = src;
            Value = value;
        }

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
}
