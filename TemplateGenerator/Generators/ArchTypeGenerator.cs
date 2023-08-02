using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using TemplateLanguage;

namespace TemplateGenerator
{
	class ArchTypeGenerator : ITemplateSourceGenerator<StructDeclarationSyntax>
	{
		public string Template => "ArchType.tcs";

		public Model CreateModel(StructDeclarationSyntax node)
		{
			var model = new Model();
			model.Set("namespace".AsSpan(), new Parameter<string>(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("archTypeName".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("archTypes".AsSpan(), Parameter.CreateEnum<IModel>(GetMembers(node)));

			return model;
		}

		public bool Filter(GeneratorSyntaxContext context, StructDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if ((attributeSyntax.Name as IdentifierNameSyntax).Identifier.Text == "ArchTypeAttribute")
						return true;
				}
			}

			return false;
		}

		public string GetName(StructDeclarationSyntax node)
		{
			return node.Identifier.ToString();
		}

		static Model[] GetMembers(StructDeclarationSyntax node)
		{
			var models = new List<Model>();

			foreach (var member in node.Members.Where(x => x is FieldDeclarationSyntax).Select(x => x as FieldDeclarationSyntax))
			{
				string typeName = ((member.Declaration.Type as QualifiedNameSyntax).Left as IdentifierNameSyntax).Identifier.Text;

				var model = new Model();
				model.Set("compName".AsSpan(), Parameter.Create(typeName.Split('.')[0]));

				models.Add(model);
			}

			return models.ToArray();
		}
	}
}
