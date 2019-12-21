using System.Text;

namespace Brainfuck.Core.Syntax
{
    public static class PseudoCodeGenerator
    {
        private const int IndentWidth = 4;
        public static string ToPseudoCode(this Module module) => module.Root.Accept(new Visitor());

        private class Visitor : IVisitor<string>
        {
            private int indent = 0;

            public string Visit(BlockUnitOperation op)
            {
                var sb = new StringBuilder();
                sb.AppendLine(IndentString + $"// {nameof(BlockUnitOperation)} [PtrChange = {op.PtrChange}]");
                foreach (var item in op.Operations)
                {
                    sb.AppendLine(item.Accept(this));
                }
                sb.Append(new AddPtrOperation(op.PtrChange).Accept(this));
                return sb.ToString();
            }

            public string Visit(IfTrueUnitOperation op)
            {
                var sb = new StringBuilder();
                sb.AppendLine(IndentString + $"// {nameof(IfTrueUnitOperation)} [Src = {op.Src}, PtrChange = {op.PtrChange}]");
                sb.AppendLine(IndentString + $"if ({FormatLocation(op.Src)} != 0) {{");
                indent++;
                foreach (var item in op.Operations)
                {
                    sb.AppendLine(item.Accept(this));
                }
                sb.AppendLine(new AddPtrOperation(op.PtrChange).Accept(this));
                indent--;
                sb.Append(IndentString + "}");
                return sb.ToString();
            }

            public string Visit(RoopUnitOperation op)
            {

                var sb = new StringBuilder();
                sb.AppendLine(IndentString + $"// {nameof(RoopUnitOperation)} [Src = {op.Src}, PtrChange = {op.PtrChange}]");
                sb.AppendLine(IndentString + $"while ({FormatLocation(op.Src)} != 0) {{");
                indent++;
                foreach (var item in op.Operations)
                {
                    sb.AppendLine(item.Accept(this));
                }
                sb.AppendLine(new AddPtrOperation(op.PtrChange).Accept(this));
                indent--;
                sb.Append(IndentString + "}");
                return sb.ToString();
            }

            public string Visit(AddPtrOperation op)
            {
                return IndentString + $"ptr += {op.Value};";
            }

            public string Visit(AssignOperation op)
            {
                return IndentString + $"{FormatLocation(op.Dest)} = {op.Value};";
            }

            public string Visit(AddAssignOperation op)
            {
                return IndentString + $"{FormatLocation(op.Dest)} += {op.Value};";
            }

            public string Visit(MultAddAssignOperation op)
            {
                return IndentString + $"{FormatLocation(op.Dest)} += {FormatLocation(op.Src)} * {op.Value};";
            }

            public string Visit(PutOperation op)
            {
                return IndentString + $"Put({FormatLocation(op.Src)});";
            }

            public string Visit(ReadOperation op)
            {
                return IndentString + $"{FormatLocation(op.Dest)} = Read();";
            }

            private string IndentString => new string(' ', IndentWidth * indent);
            private static string FormatLocation(MemoryLocation memoryLocation) => $"ptr[{memoryLocation.Offset}]";
        }
    }
}
