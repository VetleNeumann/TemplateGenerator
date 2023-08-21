using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateGenerator
{
	public enum NodeType
	{
		Unknown,
		Start,
		End,
		NewLine,

		TextBlock,
		CodeBlock,
		VariableBlock,
		AccessorBlock,
		EnumerableAccessorBlock,
		RepeatCodeBlock,
		NewLineBlock,
		Bracket,
		Filter,

		Integer,
		Float,
		Bool,

		Add,
		Subtract,
		Multiply,
		Divide,

		Assign,
		Accessor,
		Conditional,

		Variable,
		String,

		If,
		Else,
		Equals,
		Greater,
		Less,
		And,
		Or,
	}
}
