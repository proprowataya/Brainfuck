using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Brainfuck.Core
{
    public interface IOperation
    {
        IOperation WithAddLocation(int delta);
        int MaxAccessOffset { get; }
    }

    internal interface IWriteOperation : IOperation
    {
        MemoryLocation Dest { get; }
    }

    internal interface IReadOperation : IOperation
    {
        MemoryLocation Src { get; }
    }

    #region Operations

    public sealed class AddPtr : IOperation
    {
        public int Value { get; }

        public AddPtr(int value)
        {
            Value = value;
        }

        public IOperation WithAddLocation(int delta) => this;
        public int MaxAccessOffset => 0;

        public override string ToString() => $"{nameof(AddPtr)} {Value}";
    }

    public sealed class AddValue : IWriteOperation
    {
        public MemoryLocation Dest { get; }
        public int Value { get; }

        public AddValue(MemoryLocation dest, int value)
        {
            Dest = dest;
            Value = value;
        }

        public IOperation WithAddLocation(int delta) => new AddValue(Dest.WithAdd(delta), Value);
        public int MaxAccessOffset => Dest.Offset;

        public override string ToString() => $"{nameof(AddValue)} {Dest}, {Value}";
    }

    public sealed class MultAdd : IReadOperation, IWriteOperation
    {
        public MemoryLocation Dest { get; }
        public MemoryLocation Src { get; }
        public int Value { get; }

        public MultAdd(MemoryLocation dest, MemoryLocation src, int value)
        {
            Dest = dest;
            Src = src;
            Value = value;
        }

        public IOperation WithAddLocation(int delta) => new MultAdd(Dest.WithAdd(delta), Src.WithAdd(delta), Value);
        public int MaxAccessOffset => Math.Max(Dest.Offset, Src.Offset);

        public override string ToString() => $"{nameof(MultAdd)} {Dest}, {Src}, {Value}";
    }

    public sealed class Assign : IWriteOperation
    {
        public MemoryLocation Dest { get; }
        public int Value { get; }

        public Assign(MemoryLocation dest, int value)
        {
            Dest = dest;
            Value = value;
        }

        public IOperation WithAddLocation(int delta) => new Assign(Dest.WithAdd(delta), Value);
        public int MaxAccessOffset => Dest.Offset;

        public override string ToString() => $"{nameof(Assign)} {Dest}, {Value}";
    }

    public sealed class Put : IReadOperation
    {
        public MemoryLocation Src { get; }

        public Put(MemoryLocation src)
        {
            Src = src;
        }

        public IOperation WithAddLocation(int delta) => new Put(Src.WithAdd(delta));
        public int MaxAccessOffset => Src.Offset;

        public override string ToString() => $"{nameof(Put)} {Src}";
    }

    public sealed class Read : IWriteOperation
    {
        public MemoryLocation Dest { get; }

        public Read(MemoryLocation dest)
        {
            Dest = dest;
        }

        public IOperation WithAddLocation(int delta) => new Read(Dest.WithAdd(delta));
        public int MaxAccessOffset => Dest.Offset;

        public override string ToString() => $"{nameof(Read)} {Dest}";
    }

    public sealed class Roop : IOperation
    {
        public ImmutableArray<IOperation> Operations { get; set; }

        public Roop(ImmutableArray<IOperation> operations)
        {
            Operations = operations;
        }

        public IOperation WithAddLocation(int delta) => this;
        public int MaxAccessOffset => 0;

        public override string ToString() => $"{nameof(Roop)} Length: {Operations.Length}";
    }

    public sealed class IfTrue : IReadOperation
    {
        public MemoryLocation Condition { get; }
        public ImmutableArray<IOperation> Operations { get; set; }

        public IfTrue(MemoryLocation condition, ImmutableArray<IOperation> operations)
        {
            Condition = condition;
            Operations = operations;
        }

        public IOperation WithAddLocation(int delta) => new IfTrue(Condition.WithAdd(delta), Operations);
        public int MaxAccessOffset => Condition.Offset;

        public override string ToString() => $"{nameof(IfTrue)} Length: {Operations.Length}";
        MemoryLocation IReadOperation.Src => Condition;
    }

    public sealed class DummyWriteOp : IWriteOperation
    {
        public MemoryLocation Dest { get; }

        public DummyWriteOp(MemoryLocation dest)
        {
            Dest = dest;
        }

        public IOperation WithAddLocation(int delta) => new DummyWriteOp(Dest.WithAdd(delta));
        public int MaxAccessOffset => Dest.Offset;
    }

    #endregion

    public struct MemoryLocation
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
