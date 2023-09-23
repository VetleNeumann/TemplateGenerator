using LightLexer;
using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace TemplateGenerator
{

	public static class TemplateGeneratorHelpers
	{
		static Dictionary<EngineState, IState<NodeType, EngineState>> stateDict = new()
		{
			{ EngineState.TextState,  new TextState() },
			{ EngineState.Expression, new ExpressionState() },
			{ EngineState.Code,       new CodeState() },
			{ EngineState.Variable,   new VariableState() },
		};

		public static void RegisterTemplateGenerator<TNode>(IncrementalGeneratorInitializationContext context, ITemplateSourceGenerator<TNode> generator) where TNode : SyntaxNode
		{
			var generatorNodes = context.SyntaxProvider
				.CreateSyntaxProvider(
					(x, _) => x is TNode t && generator.Filter(t),
					(x, _) => x.Node as TNode
				).Where(x => x is not null)
				.Collect();

			var combinaton = context.CompilationProvider.Combine(generatorNodes);
			context.RegisterSourceOutput(combinaton, (spc, source) => ExecuteGenerator(source.Left, source.Right, spc, generator));
		}

		public static void ExecuteGenerator<TNode>(Compilation compilation, ImmutableArray<TNode> nodeArray, SourceProductionContext generatorContext, ITemplateSourceGenerator<TNode> generator) where TNode : SyntaxNode
		{
			if (nodeArray.IsDefaultOrEmpty)
				return;

			var template = generator.Template.AsSpan();

			foreach (TNode node in nodeArray.Distinct())
			{
				ModelStack<ReturnType> stack = new ModelStack<ReturnType>();

				Model<ReturnType> model = generator.CreateModel(compilation, node);
				stack.Push(model);

				var result = RenderTemplate(template, stack);
				generatorContext.AddSource($"{generator.GetName(node)}.g.cs", SourceText.From(result, Encoding.UTF8));
			}
		}

		public static string RenderTemplate(ReadOnlySpan<char> template, ModelStack<ReturnType> stack)
		{
			StringBuilder sb = new StringBuilder();

			TokenEnumerable tokens = new TemplateRules().GetEnumerable(template);

			Parser<NodeType, EngineState> parser = new(stateDict, tokens);
			TypeResolver<NodeType, ReturnType> resolver = new(TypeResolver.ResolveType);

			var nodeArr = ArrayPool<Node<NodeType>>.Shared.Rent(4096);
			var typeArr = ArrayPool<ReturnType>.Shared.Rent(4096);

			var ast = parser.GetAst(nodeArr.AsSpan());
			int start = ast.InsertNode(NodeType.Start);
			ast.SetRight(start);

			parser.CalculateAst(ref ast, EngineState.TextState);

			var types = resolver.ResolveTypes(ast.GetRoot(), ast.GetTree(), typeArr);

			TemplateContext<NodeType, ReturnType> context = new()
			{
				txt = template,
				nodes = ast.GetTree(),
				returnTypes = types
			};

			var result = TemplateLanguageRules.Compute(ref context, 0, sb, stack);

			if (!result.Ok)
				throw new Exception("Template language was not ok!");

			ArrayPool<Node<NodeType>>.Shared.Return(nodeArr);
			ArrayPool<ReturnType>.Shared.Return(typeArr);

			return sb.ToString();
		}
	}
}
