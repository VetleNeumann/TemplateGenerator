using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using LightParser;
using System;
using System.Collections.Generic;

namespace TemplateGenerator
{
	public interface ITemplateSourceGenerator<TNode> where TNode : SyntaxNode
	{
		string Template { get; }

		bool Filter(TNode node);

		//Model<ReturnType> CreateModel(Compilation compilation, TNode node);

		bool TryCreateModel(Compilation compilation, TNode node, out Model<ReturnType> model, out List<Diagnostic> diagnostics);

		string GetName(TNode node);
	}
}
