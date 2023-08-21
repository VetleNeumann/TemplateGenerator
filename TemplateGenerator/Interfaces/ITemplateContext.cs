using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace TemplateGenerator
{
	interface ITemplateContext<TContext>
	{
		TContext Context { get; }

		void Invalidate(Guid id);

		void Execute<TNode>(ImmutableArray<TNode> nodeArray, SourceProductionContext generatorContext, ITemplateSourceGenerator<TNode, TContext> generator) where TNode : SyntaxNode;

		void ExecuteInvalidated();
	}
}
