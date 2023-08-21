using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using LightParser;
using System;

namespace TemplateGenerator
{
	interface ITemplateSourceGenerator<TNode, TContext> where TNode : SyntaxNode
	{
		Guid Id { get; }

		string Template { get; }

		bool Filter(GeneratorSyntaxContext context, TNode node);

		Model<ReturnType> CreateModel(TNode node, ITemplateContext<TContext> context);

		string GetName(TNode node);
	}
}
