using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using TemplateLanguage;
using Tokhenizer;

namespace TemplateGenerator
{

	static class TemplateGeneratorHelpers
	{
		static TemplateRules rules = new();

		public static void RegisterTemplateGenerator<T>(IncrementalGeneratorInitializationContext context, ITemplateSourceGenerator<T> generator) where T : SyntaxNode
		{
			var nodes = context.SyntaxProvider
				.CreateSyntaxProvider(
					(x, _) => x is T t,
					(x, _) => generator.Filter(x, (T)x.Node) ? (T)x.Node : null
				).Where(x => x is not null);

			var combinaton = context.CompilationProvider.Combine(nodes.Collect());
			context.RegisterSourceOutput(combinaton, (spc, source) => Execute(source.Left, source.Right, spc, generator));
		}

		static void Execute<T>(Compilation compilation, ImmutableArray<T> nodeArray, SourceProductionContext context, ITemplateSourceGenerator<T> generator) where T : SyntaxNode
		{
			if (nodeArray.IsDefaultOrEmpty)
				return;

			var template = ResourceReader.GetResource(generator.Template).AsSpan();

			foreach (T node in nodeArray.Distinct())
			{
				ModelStack stack = new ModelStack();

				Model model = generator.CreateModel(node);
				stack.Push(model);

				var result = RenderTemplate(template, stack);
				context.AddSource(generator.GetName(node), SourceText.From(result, Encoding.UTF8));
			}
		}

		static string RenderTemplate(ReadOnlySpan<char> template, ModelStack stack)
		{
			StringBuilder sb = new StringBuilder();

			var ast = new ParsedTemplate(template, rules.GetEnumerable(template));
			ast.RenderTo(sb, stack);

			return sb.ToString();
		}

		public static string GetNamespace(SyntaxNode syntax)
		{
			// If we don't have a namespace at all we'll return an empty string
			// This accounts for the "default namespace" case
			string nameSpace = string.Empty;

			// Get the containing syntax node for the type declaration
			// (could be a nested type, for example)
			SyntaxNode? potentialNamespaceParent = syntax.Parent;

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
	}

	[Generator]
	public class TemplateGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, new ComponentGenerator());
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, new ArchTypeGenerator());
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, new SystemGenerator());
		}
	}
}
