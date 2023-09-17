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

	static class TemplateGeneratorHelpers
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

			var template = ResourceReader.GetResource(generator.Template).AsSpan();

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

	[Generator]
	public class TemplateGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var ecsGenerator = new EcsGenerator();
			var compGenerator = new ComponentGenerator();
			var archTypeGenerator = new ArchTypeGenerator();
			var systemGenerator = new SystemGenerator();
			var worldGenerator = new WorldGenerator();

			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, compGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, archTypeGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, systemGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, worldGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, ecsGenerator);
		}
	}

	public static class GeneratorExtensions
	{
		public static string GetNamespace(this SyntaxNode syntax)
		{
			// If we don't have a namespace at all we'll return an empty string
			// This accounts for the "default namespace" case
			string nameSpace = string.Empty;

			// Get the containing syntax node for the type declaration
			// (could be a nested type, for example)
			SyntaxNode potentialNamespaceParent = syntax.Parent;

			// Keep moving "out" of nested classes etc until we get to a namespace
			// or until we run out of parents
			while (potentialNamespaceParent != null &&
					potentialNamespaceParent is not NamespaceDeclarationSyntax
					&& potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
			{
				potentialNamespaceParent = potentialNamespaceParent.Parent;
			}

			// Build up the final namespace by looping until we no longer have a namespace declaration
			if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
			{
				// We have a namespace. Use that as the type
				nameSpace = namespaceParent.Name.ToString();

				// Keep moving "out" of the namespace declarations until we 
				// run out of nested namespace declarations
				while (true)
				{
					if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
					{
						break;
					}

					// Add the outer namespace as a prefix to the final namespace
					nameSpace = $"{namespaceParent.Name}.{nameSpace}";
					namespaceParent = parent;
				}
			}

			// return the final namespace
			return nameSpace;
		}

		public static T FindNode<T>(this IEnumerable<SyntaxNode> nodes, Func<T, bool> predicate) where T : SyntaxNode
		{
			return nodes.Where(x => x is T).Cast<T>().Single(predicate);
		}
	}
}
