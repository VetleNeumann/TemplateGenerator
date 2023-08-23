using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using LightParser;
using System;

namespace TemplateGenerator
{
	interface ITemplateSourceGenerator<TNode> where TNode : SyntaxNode
	{
		string Template { get; }

		bool Filter(GeneratorSyntaxContext context, TNode node);

		Model<ReturnType> CreateModel(Compilation compilation, TNode node);

		string GetName(TNode node);
	}
}
