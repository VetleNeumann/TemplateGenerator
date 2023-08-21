using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TemplateGenerator
{
	class ComponentGenerator : ITemplateSourceGenerator<StructDeclarationSyntax, EcsContext>
	{
		public Guid Id { get; } = Guid.NewGuid();

		public string Template => "Component.tcs";

		public bool Filter(GeneratorSyntaxContext context, StructDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if ((attributeSyntax.Name as IdentifierNameSyntax).Identifier.Text == "ComponentAttribute")
						return true;

					if ((attributeSyntax.Name as IdentifierNameSyntax).Identifier.Text == "Component")
						return true;
				}
			}

			return false;
		}

		public Model<ReturnType> CreateModel(StructDeclarationSyntax node, ITemplateContext<EcsContext> context)
		{
			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), new Parameter<string>(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("members".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetMembers(node)));

			return model;
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
				string typeName = GetTypeName(member.Declaration.Type);

				var model = new Model<ReturnType>();
				model.Set("name".AsSpan(), Parameter.Create(member.Declaration.Variables[0].ToString()));
				model.Set("type".AsSpan(), Parameter.Create(typeName));

				models.Add(model);
			}

			return models.ToArray();
		}

		static string GetTypeName<T>(T type) where T : TypeSyntax
		{
			if (type is PredefinedTypeSyntax predefined)
			{
				return predefined.Keyword.Text;
			}
			else if (type is GenericNameSyntax generic)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(generic.Identifier.Text);

				if (generic.TypeArgumentList.Arguments.Count > 0)
				{
					sb.Append('<');
					foreach (TypeSyntax arg in generic.TypeArgumentList.Arguments)
					{
						sb.Append(GetTypeName(arg));
					}
					sb.Append('>');
				}

				return sb.ToString();
			}
			else
			{
				throw new Exception("Unexpected type declaration");
			}
		}
	}
}
