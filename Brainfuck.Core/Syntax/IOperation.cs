using System.Collections.Immutable;
using System.Linq;

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
        IUnitOperation WithOperations(ImmutableArray<IOperation> newOperations);
        IUnitOperation WithPtrChange(int newPtrChange);
    }

    internal interface IAssignOperation : IWriteOperation
    { }

    #region UnitOperations

    public sealed record BlockUnitOperation(ImmutableArray<IOperation> Operations, int PtrChange) : IUnitOperation
    {
        public IOperation WithAdd(int delta) =>
            this with { Operations = Operations.Select(o => o.WithAdd(delta)).ToImmutableArray() };

        public IUnitOperation WithOperations(ImmutableArray<IOperation> newOperations) => this with { Operations = newOperations };
        public IUnitOperation WithPtrChange(int newPtrChange) => this with { PtrChange = newPtrChange };

        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed record IfTrueUnitOperation(ImmutableArray<IOperation> Operations, int PtrChange, MemoryLocation Src) : IUnitOperation, IReadOperation
    {
        public IOperation WithAdd(int delta) =>
            this with { Operations = Operations.Select(o => o.WithAdd(delta)).ToImmutableArray(), Src = Src.WithAdd(delta) };

        public IUnitOperation WithOperations(ImmutableArray<IOperation> newOperations) => this with { Operations = newOperations };
        public IUnitOperation WithPtrChange(int newPtrChange) => this with { PtrChange = newPtrChange };

        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed record RoopUnitOperation(ImmutableArray<IOperation> Operations, int PtrChange, MemoryLocation Src) : IUnitOperation, IReadOperation
    {
        public IOperation WithAdd(int delta) =>
            this with { Operations = Operations.Select(o => o.WithAdd(delta)).ToImmutableArray(), Src = Src.WithAdd(delta) };

        public IUnitOperation WithOperations(ImmutableArray<IOperation> newOperations) => this with { Operations = newOperations };
        public IUnitOperation WithPtrChange(int newPtrChange) => this with { PtrChange = newPtrChange };

        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    #endregion

    #region Other Operations

    public sealed record AddPtrOperation(int Value) : IOperation
    {
        public IOperation WithAdd(int delta) => this;
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed record AssignOperation(MemoryLocation Dest, int Value) : IAssignOperation
    {
        public IOperation WithAdd(int delta) => this with { Dest = Dest.WithAdd(delta) };
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed record AddAssignOperation(MemoryLocation Dest, int Value) : IAssignOperation
    {
        public IOperation WithAdd(int delta) => this with { Dest = Dest.WithAdd(delta) };
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed record MultAddAssignOperation(MemoryLocation Dest, MemoryLocation Src, int Value) : IAssignOperation, IReadOperation
    {
        public IOperation WithAdd(int delta) => this with { Dest = Dest.WithAdd(delta), Src = Src.WithAdd(delta) };
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed record PutOperation(MemoryLocation Src) : IReadOperation
    {
        public IOperation WithAdd(int delta) => this with { Src = Src.WithAdd(delta) };
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed record ReadOperation(MemoryLocation Dest) : IWriteOperation
    {
        public IOperation WithAdd(int delta) => this with { Dest = Dest.WithAdd(delta) };
        public void Accept(IVisitor visitor) => visitor.Visit(this);
        public T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    }

    #endregion
}
