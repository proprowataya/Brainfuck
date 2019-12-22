namespace Brainfuck.Core.Syntax
{
    public readonly struct MemoryLocation
    {
        public static readonly MemoryLocation Zero = new MemoryLocation(0);

        public int Offset { get; }

        public MemoryLocation(int offset)
        {
            Offset = offset;
        }

        public MemoryLocation WithAdd(int delta) => new MemoryLocation(Offset + delta);

        public static bool operator ==(MemoryLocation a, MemoryLocation b) => a.Offset == b.Offset;
        public static bool operator !=(MemoryLocation a, MemoryLocation b) => a.Offset != b.Offset;

        public override bool Equals(object obj)
        {
            if (obj is MemoryLocation l)
            {
                return Offset == l.Offset;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Offset.GetHashCode();
        }

        public override string ToString()
        {
            return Offset.ToString();
        }
    }
}
