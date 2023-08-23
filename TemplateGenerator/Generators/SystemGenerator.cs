using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateGenerator
{
	class SystemGenerator : ITemplateSourceGenerator<ClassDeclarationSyntax>
	{
		public string Template => "System.tcs";

		public Model<ReturnType> CreateModel(Compilation compilation, ClassDeclarationSyntax node)
		{
			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), new Parameter<string>(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("components".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetComponents(node)));

			return model;
		}

		public bool Filter(GeneratorSyntaxContext context, ClassDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if ((attributeSyntax.Name as SimpleNameSyntax).Identifier.Text == "SystemAttribute")
						return true;

					if ((attributeSyntax.Name as SimpleNameSyntax).Identifier.Text == "System")
						return true;
				}
			}

			return false;
		}

		public string GetName(ClassDeclarationSyntax node)
		{
			return node.Identifier.ToString();
		}

		static Model<ReturnType>[] GetComponents(ClassDeclarationSyntax node)
		{
			var models = new List<Model<ReturnType>>();

			foreach (var method in node.Members.Where(x => x is MethodDeclarationSyntax).Select(x => x as MethodDeclarationSyntax))
			{
				if (method.Identifier.Text != "Update")
					continue;

				var model = new Model<ReturnType>();
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
}
