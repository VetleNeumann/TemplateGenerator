using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using LightParser;
using System;
using System.Collections.Generic;

namespace TemplateGenerator
{
	public interface ITemplateSourceGenerator<TNode, TData> where TNode : SyntaxNode where TData : struct, IEquatable<TData>
	{
		string Template { get; }

		TData? Filter(TNode node, SemanticModel semanticModel);

		bool TryCreateModel(TData data, out Model<ReturnType> model, out List<Diagnostic> diagnostics);

		string GetName(TData data);

		Location GetLocation(TData data);
	}
}
