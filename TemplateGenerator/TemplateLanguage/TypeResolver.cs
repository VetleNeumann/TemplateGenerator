using LightParser;
using System;

namespace TemplateGenerator
{
	[Flags]
	public enum ReturnType
	{
		Unknown = 0,
		None = 1,
		Number = 2,
		Bool = 4,
		String = 8,
		Variable = 16,
		Model = 32,
		Enumerable = 64,
		Any = 127,
	}

	public static class TypeResolver
	{
		public static ReturnType ResolveType(NodeType node, ReturnType right, ReturnType middle, ReturnType left)
		{
			switch (node)
			{
				case NodeType.Start:
					return ReturnType.None;
				case NodeType.End:
					return ReturnType.None;
				case NodeType.NewLine:
					return ReturnType.None;
				case NodeType.TextBlock:
					return ReturnType.None;
				case NodeType.NewLineBlock:
					return ReturnType.None;
				case NodeType.CodeBlock:
					return ReturnType.None;
				case NodeType.RepeatCodeBlock:
					return ReturnType.None;
				case NodeType.VariableBlock:
					return ReturnType.None;
				case NodeType.Bracket:
					return right;
				case NodeType.Integer:
					return ReturnType.Number;
				case NodeType.Float:
					return ReturnType.Number;
				case NodeType.Bool:
					return ReturnType.Bool;
				case NodeType.Add:
				case NodeType.Subtract:
				case NodeType.Multiply:
				case NodeType.Divide:
					return ReturnType.Number;
				case NodeType.Equals:
				case NodeType.Greater:
				case NodeType.Less:
				case NodeType.And:
				case NodeType.Or:
					return ReturnType.Bool;
				case NodeType.Assign:
					return ReturnType.None;
				case NodeType.Variable:
					return ReturnType.Variable;
				case NodeType.String:
					return ReturnType.String;
				case NodeType.If:
					return ReturnType.None;
				case NodeType.Else:
					return ReturnType.None;
				case NodeType.Accessor:
					return ReturnType.Variable;
				case NodeType.AccessorBlock:
					return ReturnType.None;
				case NodeType.EnumerableAccessorBlock:
					return ReturnType.None;
				case NodeType.Filter:
					return right;
				case NodeType.Conditional:
					return left;
				default:
					throw new Exception("WTF!");
			}
		}
    }
}