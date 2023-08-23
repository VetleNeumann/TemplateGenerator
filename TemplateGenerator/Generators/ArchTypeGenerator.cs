using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateGenerator
{
	class ArchTypeGenerator : ITemplateSourceGenerator<StructDeclarationSyntax>
	{
		public Guid Id { get; } = Guid.NewGuid();

		public string Template => "ArchType.tcs";

        public Model<ReturnType> CreateModel(Compilation compilation, StructDeclarationSyntax node)
		{
			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("archTypeName".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("archTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetMembers(node)));

			return model;
		}

		public bool Filter(GeneratorSyntaxContext context, StructDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if ((attributeSyntax.Name as SimpleNameSyntax).Identifier.Text == "ArchTypeAttribute")
						return true;

					if ((attributeSyntax.Name as SimpleNameSyntax).Identifier.Text == "ArchType")
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

			foreach (var member in node.Members.Where(x => x is FieldDeclarationSyntax).Select(x => x as FieldDeclarationSyntax))
			{
				string typeName = ((member.Declaration.Type as QualifiedNameSyntax).Left as IdentifierNameSyntax).Identifier.Text;
				string varName = member.Declaration.Variables[0].Identifier.Text;

				var model = new Model<ReturnType>();
				model.Set("compName".AsSpan(), Parameter.Create(typeName.Split('.')[0]));
				model.Set("varName".AsSpan(), Parameter.Create(varName));

				models.Add(model);
			}

			return models.ToArray();
		}
	}
}
