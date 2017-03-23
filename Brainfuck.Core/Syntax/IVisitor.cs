namespace Brainfuck.Core.Syntax
{
    public interface IVisitor
    {
        void Visit(BlockUnitOperation op);
        void Visit(IfTrueUnitOperation op);
        void Visit(RoopUnitOperation op);
        void Visit(AddPtrOperation op);
        void Visit(AssignOperation op);
        void Visit(AddAssignOperation op);
        void Visit(MultAddAssignOperation op);
        void Visit(PutOperation op);
        void Visit(ReadOperation op);
    }

    public interface IVisitor<T>
    {
        T Visit(BlockUnitOperation op);
        T Visit(IfTrueUnitOperation op);
        T Visit(RoopUnitOperation op);
        T Visit(AddPtrOperation op);
        T Visit(AssignOperation op);
        T Visit(AddAssignOperation op);
        T Visit(MultAddAssignOperation op);
        T Visit(PutOperation op);
        T Visit(ReadOperation op);
    }
}