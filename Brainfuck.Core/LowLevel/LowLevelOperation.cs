namespace Brainfuck.Core.LowLevel
{
    public struct LowLevelOperation
    {
        public Opcode Opcode { get; }
        public int Dest { get; }
        public int Src { get; }
        public int Value { get; }

        public LowLevelOperation(Opcode opcode, int dest = 0, int src = 0, int value = 0)
        {
            Opcode = opcode;
            Dest = dest;
            Src = src;
            Value = value;
        }

        public override string ToString() => $"{Opcode} {Dest}, {Src}, {Value}";
    }

    public enum Opcode
    {
        AddPtr,
        Assign,
        AddAssign,
        MultAddAssign,
        Put,
        Read,
        BrTrue,
        BrFalse,
        EnsureBuffer,
    }
}
