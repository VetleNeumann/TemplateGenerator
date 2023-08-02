using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using TemplateLanguage;

namespace TemplateGenerator
{
	interface ITemplateSourceGenerator<T> where T : SyntaxNode
	{
		string Template { get; }

		bool Filter(GeneratorSyntaxContext context, T node);

		Model CreateModel(T node);

		string GetName(T node);
	}
}
