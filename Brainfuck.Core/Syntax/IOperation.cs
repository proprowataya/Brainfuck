using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Brainfuck.Core.Syntax
{
    public interface IOperation
    {
        IOperation WithAdd(int delta);
        void Accept(IVisitor visitor);
        T Accept<T>(IVisitor<T> visitor);
    }

    public interface IReadOperation : IOperation
    {
        MemoryLocation Src { get; }
    }

    public interface IWriteOperation : IOperation
    {
        MemoryLocation Dest { get; }
    }

    public interface IUnitOperation : IOperation
    {
        ImmutableArray<IOperation> Operations { get; }
        int PtrChange { get; }
    }

    internal interface IAssignOperation : IWriteOperation
    { }

    #region UnitOperations

    public sealed class BlockUnitOperation : IUnitOperation
    {
        public ImmutableArray<IOperation> Operations { get; }
        public int PtrChange { get; }

        public BlockUnitOperation(ImmutableArray<IOperation> operations, int ptrChange)
        {
            Operations = operations;
            PtrChange = ptrChange;
        }

        public IEnumerable<IReadOperation> EnumerateReadOperations => Operations.OfType<IReadOperation>();
        public IEnumerable<IWriteOperation> EnumerateWriteOperations => Operations.OfType<IWriteOperation>();

        public IOperation WithAdd(int delta) =>
            new BlockUnitOperation(Operations.Select(o => o.WithAdd(delta)).ToImmutableArray(), PtrChange);
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class IfTrueUnitOperation : IUnitOperation, IReadOperation
    {
        public ImmutableArray<IOperation> Operations { get; }
        public int PtrChange { get; }
        public MemoryLocation Src { get; }

        public IfTrueUnitOperation(ImmutableArray<IOperation> operations, int ptrChange, MemoryLocation src)
        {
            Operations = operations;
            PtrChange = ptrChange;
            Src = src;
        }

        public IEnumerable<IReadOperation> EnumerateReadOperations => Operations.OfType<IReadOperation>();
        public IEnumerable<IWriteOperation> EnumerateWriteOperations => Operations.OfType<IWriteOperation>();

        public IOperation WithAdd(int delta) =>
            new IfTrueUnitOperation(Operations.Select(o => o.WithAdd(delta)).ToImmutableArray(), PtrChange, Src.WithAdd(delta));
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class RoopUnitOperation : IUnitOperation, IReadOperation
    {
        public ImmutableArray<IOperation> Operations { get; }
        public int PtrChange { get; }
        public MemoryLocation Src { get; }

        public RoopUnitOperation(ImmutableArray<IOperation> operations, int ptrChange, MemoryLocation src)
        {
            Operations = operations;
            PtrChange = ptrChange;
            Src = src;
        }

        public IEnumerable<IReadOperation> EnumerateReadOperations => Operations.OfType<IReadOperation>();
        public IEnumerable<IWriteOperation> EnumerateWriteOperations => Operations.OfType<IWriteOperation>();

        public IOperation WithAdd(int delta) =>
            new RoopUnitOperation(Operations.Select(o => o.WithAdd(delta)).ToImmutableArray(), PtrChange, Src.WithAdd(delta));
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    #endregion

    #region Other Operations

    public sealed class AddPtrOperation : IOperation
    {
        public int Value { get; }

        public AddPtrOperation(int value)
        {
            Value = value;
        }

        public IOperation WithAdd(int delta) => this;
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class AssignOperation : IAssignOperation
    {
        public MemoryLocation Dest { get; }
        public int Value { get; }

        public AssignOperation(MemoryLocation dest, int value)
        {
            Dest = dest;
            Value = value;
        }

        public IOperation WithAdd(int delta) => new AssignOperation(Dest.WithAdd(delta), Value);
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class AddAssignOperation : IAssignOperation
    {
        public MemoryLocation Dest { get; }
        public int Value { get; }

        public AddAssignOperation(MemoryLocation dest, int value)
        {
            Dest = dest;
            Value = value;
        }

        public IOperation WithAdd(int delta) => new AddAssignOperation(Dest.WithAdd(delta), Value);
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class MultAddAssignOperation : IAssignOperation, IReadOperation
    {
        public MemoryLocation Src { get; }
        public MemoryLocation Dest { get; }
        public int Value { get; }

        public MultAddAssignOperation(MemoryLocation dest, int value)
        {
            Src = Src;
            Dest = dest;
            Value = value;
        }

        public IOperation WithAdd(int delta) => new AssignOperation(Dest.WithAdd(delta), Value);
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class PutOperation : IReadOperation
    {
        public MemoryLocation Src { get; }

        public PutOperation(MemoryLocation src)
        {
            Src = src;
        }

        public IOperation WithAdd(int delta) => new PutOperation(Src.WithAdd(delta));
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class ReadOperation : IWriteOperation
    {
        public MemoryLocation Dest { get; }

        public ReadOperation(MemoryLocation dest)
        {
            Dest = dest;
        }

        public IOperation WithAdd(int delta) => new ReadOperation(Dest.WithAdd(delta));
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    #endregion
}
