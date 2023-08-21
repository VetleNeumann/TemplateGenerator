using System;
using LightLexer;
using LightParser;

namespace TemplateGenerator
{
	public class CodeState : IState<NodeType, EngineState>
	{
		public ExitCode OnStep(ref Parser<NodeType, EngineState> sm, ref AbstractSyntaxTree<NodeType> ast, ref Token token)
		{
            if (
				token.Is(TokenType.Bracket, BracketType.Code) ||
				token.Is(TokenType.Operator, OperatorType.Then) ||
				token.Is(TokenType.Operator, OperatorType.Else) ||
				token.Is(TokenType.Operator, OperatorType.Elseif) ||
				token.Is(TokenType.Operator, OperatorType.End))
			{
				ast.InsertNode(NodeType.End);
				return sm.PopState();
			}

			if (token.Is(TokenType.Whitespace))
				return sm.Continue();

			if (token.Is(TokenType.Number) || token.Is(TokenType.Operator, OperatorType.Variable) || token.Is(TokenType.Bracket, BracketType.Open))
			{
				ast.BracketOpen(NodeType.Bracket);
				sm.Transition(EngineState.Expression, ref ast, repeatToken: true);
				ast.BracketClose();

				return OnStep(ref sm, ref ast, ref token);
			}
			else if (token.Is(TokenType.Bracket, BracketType.String))
			{
				Range strRange = new Range(token.range.Start.Value + 1, token.range.End.Value + 1);
				sm.Consume();

				while (!token.Is(TokenType.Bracket, BracketType.String))
				{
					strRange = new Range(strRange.Start, token.range.End);
					sm.Consume();
				}

				ast.InsertNode(NodeType.String, new Token(strRange));
				return sm.Continue();
			}
			else if (token.Is(TokenType.Operator))
			{
				OperatorType type = token.Get<OperatorType>(1);
				switch (type)
				{
					case OperatorType.Asssign:
						InsertOperator(ref ast, NodeType.Assign);
						break;
					case OperatorType.Equals:
						InsertOperator(ref ast, NodeType.Equals);
						break;
					case OperatorType.And:
						InsertOperator(ref ast, NodeType.And);
						break;
					case OperatorType.Or:
						InsertOperator(ref ast, NodeType.Or);
						break;
					case OperatorType.Greater:
						InsertOperator(ref ast, NodeType.Greater);
						break;
					case OperatorType.Less:
						InsertOperator(ref ast, NodeType.Less);
						break;
					case OperatorType.Conditional:
						InsertOperator(ref ast, NodeType.Conditional);
						break;
					case OperatorType.If:
						int ifIdx = 0;

						do
						{
							// If token is elseif it means there is a previous if statement
							if (token.Is(TokenType.Operator, OperatorType.Elseif))
								ast.SetRight(ifIdx);

							ifIdx = ast.InsertNode(NodeType.If);
							ast.SetLeft(ifIdx) ;

							ast.BracketOpen(NodeType.Bracket);
							sm.Transition(EngineState.Code, ref ast, repeatToken: false);
							ast.BracketClose();

							if (!token.Is(TokenType.Operator, OperatorType.Then))
								throw new Exception("If condition must be closed by then");

							ast.SetMiddle(ifIdx);

							ast.BracketOpen(NodeType.Bracket);
							sm.Transition(EngineState.Code, ref ast, repeatToken: false);
							ast.BracketClose();
						}
						while (token.Is(TokenType.Operator, OperatorType.Elseif));

						if (token.Is(TokenType.Operator, OperatorType.Else))
						{
                            ast.SetRight(ifIdx);

							ast.BracketOpen(NodeType.Bracket);
							sm.Transition(EngineState.Code, ref ast, repeatToken: false);
							ast.BracketClose();
						}

						if (!token.Is(TokenType.Operator, OperatorType.End))
							throw new Exception("If statement must be closed with end.");

						return sm.Continue();
					default:
						break;
				}
			}
			else if (token.Is(TokenType.NewLine))
			{
				InsertOperator(ref ast, NodeType.NewLine);

				ast.BracketOpen(NodeType.Bracket);
				sm.Transition(EngineState.Code, ref ast, repeatToken: false);
				ast.BracketClose();

				return OnStep(ref sm, ref ast, ref token);
			}
			else if (token.Is(TokenType.Bool))
			{
				ast.InsertNode(NodeType.Bool, token);
			}
			else if (token.Is(TokenType.String))
			{
				ast.InsertNode(NodeType.String, token);
			}

			return sm.Continue();
		}

		void InsertOperator(ref AbstractSyntaxTree<NodeType> ast, NodeType type)
		{
			int accessor = ast.TakeLeft(type);
			ast.SetRight(accessor);
		}
	}
}