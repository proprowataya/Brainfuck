using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Brainfuck.Core.Syntax
{
    public interface INode
    {
        IEnumerable<MemoryLocation> AccessLocations { get; }
        void Accept(IVisitor visitor);
    }

    public interface IStatement : INode
    {
        IStatement WithAdd(int delta);
    }

    public interface IUnitStatement : IStatement
    {
        ImmutableArray<IStatement> Statements { get; }
        int OffsetChange { get; }
    }

    public interface IExpression : INode
    {
        IExpression WithAdd(int delta);
    }

    #region Statements

    #region UnitStatements

    public sealed class BlockUnit : IUnitStatement
    {
        public ImmutableArray<IStatement> Statements { get; }
        public int OffsetChange { get; }

        public BlockUnit(ImmutableArray<IStatement> statements, int offsetChange)
        {
            Statements = statements;
            OffsetChange = offsetChange;
        }

        public IStatement WithAdd(int delta)
        {
            var builder = ImmutableArray.CreateBuilder<IStatement>(Statements.Length);

            for (int i = 0; i < Statements.Length; i++)
            {
                builder.Add(Statements[i].WithAdd(delta));
            }

            return new BlockUnit(builder.MoveToImmutable(), OffsetChange);
        }

        public IEnumerable<MemoryLocation> AccessLocations => Statements.SelectMany(s => s.AccessLocations);
        public void Accept(IVisitor visitor) => visitor.Visit(this);
    }

    public sealed class IfTrueUnit : IUnitStatement
    {
        public ImmutableArray<IStatement> Statements { get; }
        public int OffsetChange { get; }
        public MemoryLocation Src { get; }

        public IfTrueUnit(ImmutableArray<IStatement> statements, int offsetChange, MemoryLocation src)
        {
            Statements = statements;
            OffsetChange = offsetChange;
            Src = src;
        }

        public IStatement WithAdd(int delta)
        {
            var builder = ImmutableArray.CreateBuilder<IStatement>(Statements.Length);

            for (int i = 0; i < Statements.Length; i++)
            {
                builder.Add(Statements[i].WithAdd(delta));
            }

            return new IfTrueUnit(builder.MoveToImmutable(), OffsetChange, Src.WithAdd(delta));
        }

        public IEnumerable<MemoryLocation> AccessLocations => Statements.SelectMany(s => s.AccessLocations).Concat(new[] { Src });
        public void Accept(IVisitor visitor) => visitor.Visit(this);
    }

    public sealed class RoopUnit : IUnitStatement
    {
        public ImmutableArray<IStatement> Statements { get; }
        public int OffsetChange { get; }
        public MemoryLocation Src { get; }

        public RoopUnit(ImmutableArray<IStatement> statements, int offsetChange, MemoryLocation src)
        {
            Statements = statements;
            OffsetChange = offsetChange;
            Src = src;
        }

        public IStatement WithAdd(int delta)
        {
            var builder = ImmutableArray.CreateBuilder<IStatement>(Statements.Length);

            for (int i = 0; i < Statements.Length; i++)
            {
                builder.Add(Statements[i].WithAdd(delta));
            }

            return new RoopUnit(builder.MoveToImmutable(), OffsetChange, Src.WithAdd(delta));
        }

        public IEnumerable<MemoryLocation> AccessLocations => Statements.SelectMany(s => s.AccessLocations).Concat(new[] { Src });
        public void Accept(IVisitor visitor) => visitor.Visit(this);
    }

    #endregion

    public sealed class AssignStatement : IStatement
    {
        public MemoryLocation Dest { get; }
        public IExpression Expression { get; }

        public AssignStatement(MemoryLocation dest, IExpression expression)
        {
            Dest = dest;
            Expression = expression;
        }

        public IStatement WithAdd(int delta) => new AssignStatement(Dest.WithAdd(delta), Expression.WithAdd(delta));
        public IEnumerable<MemoryLocation> AccessLocations => new[] { Dest }.Concat(Expression.AccessLocations);
        public void Accept(IVisitor visitor) => visitor.Visit(this);
    }

    public sealed class PutStatement : IStatement
    {
        public IExpression Src { get; }

        public PutStatement(IExpression src)
        {
            Src = src;
        }

        public IStatement WithAdd(int delta) => new PutStatement(Src.WithAdd(delta));
        public IEnumerable<MemoryLocation> AccessLocations => Src.AccessLocations;
        public void Accept(IVisitor visitor) => visitor.Visit(this);
    }

    #endregion

    #region Expressions

    public sealed class ConstExpression : IExpression
    {
        public static readonly ConstExpression Zero = new ConstExpression(0);
        public static readonly ConstExpression One = new ConstExpression(1);
        public static readonly ConstExpression MinusOne = new ConstExpression(-1);

        public int Value { get; }

        public ConstExpression(int value)
        {
            Value = value;
        }

        public IExpression WithAdd(int delta) => this;
        public IEnumerable<MemoryLocation> AccessLocations => Enumerable.Empty<MemoryLocation>();
        public void Accept(IVisitor visitor) => visitor.Visit(this);
    }

    public sealed class MemoryAccessExpression : IExpression
    {
        public MemoryLocation Src { get; }

        public MemoryAccessExpression(MemoryLocation src)
        {
            Src = src;
        }

        public IExpression WithAdd(int delta) => new MemoryAccessExpression(Src.WithAdd(delta));
        public IEnumerable<MemoryLocation> AccessLocations => new[] { Src };
        public void Accept(IVisitor visitor) => visitor.Visit(this);
    }

    public sealed class GetExpression : IExpression
    {
        public static readonly GetExpression Instance = new GetExpression();

        private GetExpression()
        { }

        public IExpression WithAdd(int delta) => this;
        public IEnumerable<MemoryLocation> AccessLocations => Enumerable.Empty<MemoryLocation>();
        public void Accept(IVisitor visitor) => visitor.Visit(this);
    }

    public sealed class AddExpression : IExpression
    {
        public IExpression Left { get; }
        public IExpression Right { get; }

        public AddExpression(IExpression left, IExpression right)
        {
            Left = left;
            Right = right;
        }

        public IExpression WithAdd(int delta) => new AddExpression(Left.WithAdd(delta), Right.WithAdd(delta));
        public IEnumerable<MemoryLocation> AccessLocations => Left.AccessLocations.Concat(Right.AccessLocations);
        public void Accept(IVisitor visitor) => visitor.Visit(this);
    }

    public sealed class MultiplyExpression : IExpression
    {
        public IExpression Left { get; }
        public IExpression Right { get; }

        public MultiplyExpression(IExpression left, IExpression right)
        {
            Left = left;
            Right = right;
        }

        public IExpression WithAdd(int delta) => new MultiplyExpression(Left.WithAdd(delta), Right.WithAdd(delta));
        public IEnumerable<MemoryLocation> AccessLocations => Left.AccessLocations.Concat(Right.AccessLocations);
        public void Accept(IVisitor visitor) => visitor.Visit(this);
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
