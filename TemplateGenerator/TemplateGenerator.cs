using LightLexer;
using LightLexer.Helpers;
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
	static class DiagnosticDescriptors
	{
		public static readonly DiagnosticDescriptor TemplateFailed = new("TGEN001", "Template failed to render", "Template '{0}' failed with error: {1}", "Renderer", DiagnosticSeverity.Error, true);
	}


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

				bool modelResult;
				List<Diagnostic> diagnostics;
				Model<ReturnType> model;
				try
				{
					modelResult = generator.TryCreateModel(compilation, node, out model, out diagnostics);
				}
				catch (Exception e)
				{
                    throw new Exception($"Generator '{generator.GetName(node)}' failed with:\n {e.ToString()}");
				}
				
				foreach (var diagnostic in diagnostics.GroupBy(x => (x.Id, x.Location)).Select(x => x.First()))
					generatorContext.ReportDiagnostic(diagnostic);

				if (!modelResult)
					continue;

				stack.Push(model);

				var renderResult = TryRenderTemplate(template, stack, out string result);
				if (!renderResult.Ok)
				{
					var errorSb = new StringBuilder("\n");
					for (int i = 0; i < renderResult.Errors.Count; i++)
					{
						errorSb.Append($"\t{renderResult.Lines[i]}: {renderResult.Errors[i]}\n");
					}

					generatorContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TemplateFailed, node.GetLocation(), $"{generator.GetName(node)}.g.cs", errorSb.ToString()));
				}

				generatorContext.AddSource($"{generator.GetName(node)}.g.cs", SourceText.From(result, Encoding.UTF8));
			}
		}

		public static ComputeResult TryRenderTemplate(ReadOnlySpan<char> template, ModelStack<ReturnType> stack, out string result)
		{
			StringBuilder sb = new StringBuilder();

			TokenEnumerable tokens = new TemplateRules().GetEnumerable(template);

			Parser<NodeType, EngineState> parser = new(stateDict, tokens);
			TypeResolver<NodeType, ReturnType> resolver = new(TypeResolver.ResolveType);

			var nodeArr = ArrayPool<Node<NodeType>>.Shared.Rent(4096 * 2);
			var typeArr = ArrayPool<ReturnType>.Shared.Rent(4096 * 2);

			var ast = parser.GetAst(nodeArr.AsSpan());
			int start = ast.InsertNode(NodeType.Start);
			ast.SetRight(start);

			parser.CalculateAst(ref ast, EngineState.TextState);

			bool hasLoops = VeriyTree(ast.GetTree(), out int idx);

			if (!hasLoops)
			{
				ref Node<NodeType> node = ref ast.GetTree()[idx];
				var str = node.token.GetSpan(template).ToString();

				var pointers = FindAllPointers(idx, ast.GetTree());

				Console.WriteLine($"Idx: {node.token.range.Start}-{node.token.range.End}, '{str}'");
                Console.WriteLine("Template:");
				for (int i = 0; i < template.Length; i++)
				{
					if (IsWithin(node.token.range, i))
					{
						Console.BackgroundColor = ConsoleColor.Red;
					}

					for (int a = 0; a < pointers.Count; a++)
					{
						if (IsWithin(ast.GetTree()[pointers[a]].token.range, i))
							Console.BackgroundColor = ConsoleColor.DarkCyan;
					}

					Console.Write(template[i]);
					Console.BackgroundColor = ConsoleColor.Black;
				}

                throw new Exception("Abstract Syntax Tree contains loops");
			}

			var types = resolver.ResolveTypes(ast.GetRoot(), ast.GetTree(), typeArr);

			TemplateContext<NodeType, ReturnType> context = new()
			{
				txt = template,
				nodes = ast.GetTree(),
				returnTypes = types
			};

			var computeResult = TemplateLanguageRules.Compute(ref context, 0, sb, stack);
			result = sb.ToString();

			ArrayPool<Node<NodeType>>.Shared.Return(nodeArr);
			ArrayPool<ReturnType>.Shared.Return(typeArr);

			return computeResult;
		}

		static bool IsWithin(Range range, int idx)
		{
			return idx >= range.Start.Value && idx < range.End.Value;
		}

		static List<int> FindAllPointers(int idx, ReadOnlySpan<Node<NodeType>> nodes)
		{
			List<int> pointers = new List<int>();
			for (int i = 0; i < nodes.Length; i++)
			{
				ref readonly Node<NodeType> curr = ref nodes[i];

				if (curr.right == idx || curr.middle == idx || curr.left == idx)
					pointers.Add(i);
			}

			return pointers;
		}

		static bool VeriyTree(ReadOnlySpan<Node<NodeType>> nodes, out int idx)
		{
			using RefStack<int> visited = new RefStack<int>(nodes.Length);
			visited.Push(0);

			for (int i = 0; i < nodes.Length; i++)
			{
				ref readonly Node<NodeType> curr = ref nodes[i];

				if (curr.right != -1 && SpanContains(visited.AsSpan(), curr.right))
				{
					idx = curr.right;
					return false;
				}	

				if (curr.middle != -1 && SpanContains(visited.AsSpan(), curr.middle))
				{
					idx = i;
					return false;
				}

				if (curr.left != -1 && SpanContains(visited.AsSpan(), curr.left))
				{
					idx = i;
					return false;
				}

				if (curr.right != -1)
					visited.Push(curr.right);

				if (curr.middle != -1)
					visited.Push(curr.middle);

				if (curr.left != -1)
					visited.Push(curr.left);
			}

			idx = -1;
			return true;
		}

		static bool SpanContains(ReadOnlySpan<int> span, int value)
		{
			for (int i = 0; i < span.Length; i++)
			{
				if (span[i] == value)
					return true;
			}

			return false;
		}
	}
}
