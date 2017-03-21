using System;
using System.Collections.Generic;
using System.Text;

namespace Brainfuck.Core.Syntax
{
    public static class PseudoCodeGenerator
    {
        private const int IndentWidth = 4;

        public static string ToProgramString(this INode node)
        {
            Visitor visitor = new Visitor();
            node.Accept(visitor);
            return visitor.sb.ToString().TrimEnd();
        }

        private class Visitor : IVisitor
        {
            public readonly StringBuilder sb = new StringBuilder();
            private int indent = 0;

            public void Visit(BlockUnit node)
            {
                AppendLineIndented($"// {nameof(BlockUnit)} [OffsetChange = {node.OffsetChange}]");
                foreach (var stmt in node.Statements)
                {
                    stmt.Accept(this);
                }
                AppendLineIndented(FormatChangeOffset(node.OffsetChange) + ";");
            }

            public void Visit(IfTrueUnit node)
            {
                AppendLineIndented($"// {nameof(IfTrueUnit)} [Src = {node.Src}, OffsetChange = {node.OffsetChange}]");
                AppendLineIndented($"if ({FormatLocation(node.Src)} != 0) {{");
                indent++;
                foreach (var stmt in node.Statements)
                {
                    stmt.Accept(this);
                }
                AppendLineIndented(FormatChangeOffset(node.OffsetChange) + ";");
                indent--;
                AppendLineIndented("}");
            }

            public void Visit(RoopUnit node)
            {
                AppendLineIndented($"// {nameof(RoopUnit)} [Src = {node.Src}, OffsetChange = {node.OffsetChange}]");
                AppendLineIndented($"while ({FormatLocation(node.Src)} != 0) {{");
                indent++;
                foreach (var stmt in node.Statements)
                {
                    stmt.Accept(this);
                }
                AppendLineIndented(FormatChangeOffset(node.OffsetChange) + ";");
                indent--;
                AppendLineIndented("}");
            }

            public void Visit(AssignStatement node)
            {
                AppendIndented(FormatLocation(node.Dest)).Append(" = ");
                node.Expression.Accept(this);
                sb.AppendLine(";");
            }

            public void Visit(PutStatement node)
            {
                AppendIndented($"Put(");
                node.Src.Accept(this);
                sb.AppendLine(");");
            }

            public void Visit(ConstExpression node)
            {
                sb.Append(node.Value.ToString());
            }

            public void Visit(MemoryAccessExpression node)
            {
                sb.Append(FormatLocation(node.Src));
            }

            public void Visit(GetExpression node)
            {
                sb.Append("Get()");
            }

            public void Visit(AddExpression node)
            {
                sb.Append("(");
                node.Left.Accept(this);
                sb.Append(") + (");
                node.Right.Accept(this);
                sb.Append(")");
            }

            public void Visit(MultiplyExpression node)
            {
                sb.Append("(");
                node.Left.Accept(this);
                sb.Append(") * (");
                node.Right.Accept(this);
                sb.Append(")");
            }

            private StringBuilder AppendIndented(string str)
            {
                sb.Append(' ', IndentWidth * indent).Append(str);
                return sb;
            }

            private StringBuilder AppendLineIndented(string str)
            {
                sb.Append(' ', IndentWidth * indent).AppendLine(str);
                return sb;
            }

            private static string FormatLocation(MemoryLocation memoryLocation) => $"ptr[{memoryLocation.Offset}]";
            private static string FormatChangeOffset(int offset) => offset >= 0 ? $"ptr += {offset}" : $"ptr -= {-offset}";
        }
    }
}
