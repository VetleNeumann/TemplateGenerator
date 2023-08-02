using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TemplateLanguage;

namespace TemplateGenerator
{
	class SystemGenerator : ITemplateSourceGenerator<ClassDeclarationSyntax>
	{
		public string Template => "System.tcs";

		public Model CreateModel(ClassDeclarationSyntax node)
		{
			var model = new Model();
			model.Set("namespace".AsSpan(), new Parameter<string>(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("components".AsSpan(), Parameter.CreateEnum<IModel>(GetComponents(node)));

			return model;
		}

		public bool Filter(GeneratorSyntaxContext context, ClassDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if ((attributeSyntax.Name as IdentifierNameSyntax).Identifier.Text == "SystemAttribute")
						return true;
				}
			}

			return false;
		}

		public string GetName(ClassDeclarationSyntax node)
		{
			return node.Identifier.ToString();
		}

		static Model[] GetComponents(ClassDeclarationSyntax node)
		{
			var models = new List<Model>();

			foreach (var method in node.Members.Where(x => x is MethodDeclarationSyntax).Select(x => x as MethodDeclarationSyntax))
			{
				if (method.Identifier.Text != "Update")
					continue;

				var model = new Model();
				var types = new List<string>();

				foreach (var parameter in method.ParameterList.Parameters)
				{
					var paramType = parameter.Type as QualifiedNameSyntax;
					types.Add((paramType.Left as IdentifierNameSyntax).Identifier.Text);

				}

				model.Set("compName".AsSpan(), Parameter.Create(types.Distinct().First()));

				models.Add(model);

				// TODO: Only do first method for now
				break;
			}

			return models.ToArray();
		}
	}

	class ComponentGenerator : ITemplateSourceGenerator<StructDeclarationSyntax>
	{
		public string Template => "Component.tcs";

		public bool Filter(GeneratorSyntaxContext context, StructDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if ((attributeSyntax.Name as IdentifierNameSyntax).Identifier.Text == "ComponentAttribute")
						return true;
				}
			}

			return false;
		}

		public Model CreateModel(StructDeclarationSyntax node)
		{
			var model = new Model();
			model.Set("namespace".AsSpan(), new Parameter<string>(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("members".AsSpan(), Parameter.CreateEnum<IModel>(GetMembers(node)));

			return model;
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
				string typeName = (member.Declaration.Type as PredefinedTypeSyntax).Keyword.Text;

				var model = new Model();
				model.Set("name".AsSpan(), Parameter.Create(member.Declaration.Variables[0].ToString()));
				model.Set("type".AsSpan(), Parameter.Create(typeName));

				models.Add(model);
			}

			return models.ToArray();
		}
	}
}
