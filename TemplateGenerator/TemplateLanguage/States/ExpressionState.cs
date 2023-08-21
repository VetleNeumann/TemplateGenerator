using System;
using System.Runtime.InteropServices;
using LightLexer;
using LightParser;

namespace TemplateGenerator
{

	public class ExpressionState : IState<NodeType, EngineState>
	{
		public ExitCode OnStep(ref Parser<NodeType, EngineState> sm, ref AbstractSyntaxTree<NodeType> ast, ref Token token)
		{
            if (token.Is(TokenType.Whitespace))
				return sm.Continue();

			if (token.Is(TokenType.Number))
			{
				if (token.Is(TokenType.Number, NumberType.Integer))
					ast.InsertNode(NodeType.Integer, token);

				if (token.Is(TokenType.Number, NumberType.Float))
					ast.InsertNode(NodeType.Float, token);

				return sm.Continue();
			}
			else if (token.Is(TokenType.Operator))
			{
				switch (token.Get<OperatorType>(1))
				{
					case OperatorType.Add:
						InsertOperator(ref ast, NodeType.Add);

						ast.BracketOpen(NodeType.Bracket);
						sm.Transition(EngineState.Expression, ref ast);
						ast.BracketClose();

						return OnStep(ref sm, ref ast, ref token);
					case OperatorType.Subtract:
						InsertOperator(ref ast, NodeType.Subtract);

						ast.BracketOpen(NodeType.Bracket);
						sm.Transition(EngineState.Expression, ref ast);
						ast.BracketClose();

						return OnStep(ref sm, ref ast, ref token);
					case OperatorType.Multiply:
						InsertOperator(ref ast, NodeType.Multiply);

						break;
					case OperatorType.Divide:
						InsertOperator(ref ast, NodeType.Divide);

						break;
					case OperatorType.Variable:
						if (!sm.Consume())
							return ExitCode.Exit;

						int var = ast.InsertRight(NodeType.Variable);
						ast.SetRight(var);
						ast.InsertNode(NodeType.String, token);

						break;
					case OperatorType.Accessor:
						if (!sm.Consume())
							return ExitCode.Exit;

						int accessor = ast.TakeLeft(NodeType.Accessor);
						ast.SetRight(accessor);
						ast.InsertNode(NodeType.String, token);

						break;
					default:
						return sm.PopState();
				}

				return sm.Continue();
			}
			else if (token.Is(TokenType.Bracket))
			{
				switch (token.Get<BracketType>(1))
				{
					case BracketType.Open:
                        ast.BracketOpen(NodeType.Bracket);
						sm.Transition(EngineState.Expression, ref ast, repeatToken: false);
						ast.BracketClose();

						return sm.Continue();
					default:
						return sm.PopState();
				}
			}

            return sm.PopState();
		}

		void InsertOperator(ref AbstractSyntaxTree<NodeType> ast, NodeType type)
		{
			int accessor = ast.TakeLeft(type);
			ast.SetRight(accessor);
		}
	}
}