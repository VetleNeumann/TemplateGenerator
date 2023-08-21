using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateGenerator
{
	class EcsGenerator : ITemplateSourceGenerator<StructDeclarationSyntax>
	{
		public Guid Id { get; } = Guid.NewGuid();

		public string Template => "Ecs.tcs";

		public Model<ReturnType> CreateModel(StructDeclarationSyntax node)
		{
			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), new Parameter<string>(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("archTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetMembers(node)));

			return model;
		}

		public bool Filter(GeneratorSyntaxContext context, StructDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if ((attributeSyntax.Name as IdentifierNameSyntax).Identifier.Text == "EcsAttribute")
						return true;

					if ((attributeSyntax.Name as IdentifierNameSyntax).Identifier.Text == "Ecs")
						return true;
				}
			}

			return false;
		}

		public string GetName(StructDeclarationSyntax node)
		{
			return node.Identifier.ToString();
		}

		static Model<ReturnType>[] GetMembers(StructDeclarationSyntax node)
		{

			var models = new List<Model<ReturnType>>();

			return models.ToArray();
		}
	}
}
