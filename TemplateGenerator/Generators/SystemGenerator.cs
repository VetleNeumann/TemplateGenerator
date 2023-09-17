using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace TemplateGenerator
{
	class SystemGenerator : ITemplateSourceGenerator<ClassDeclarationSyntax>
	{
		public string Template => "System.tcs";

		public Model<ReturnType> CreateModel(Compilation compilation, ClassDeclarationSyntax node)
		{
			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), new Parameter<string>(node.GetNamespace()));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("components".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetComponents(compilation, node)));
			model.Set("methods".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetMethods(node)));

			return model;
		}

		public bool Filter(ClassDeclarationSyntax node)
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

		static List<Model<ReturnType>> GetComponents(Compilation compilation, ClassDeclarationSyntax node)
		{
			var models = new List<Model<ReturnType>>();

			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());

			foreach (var method in node.Members.Where(x => x is MethodDeclarationSyntax).Select(x => x as MethodDeclarationSyntax))
			{
				if (method.Identifier.Text != "Update" && !method.Identifier.Text.StartsWith("Update"))
					continue;

				int idx = 1;
				foreach (var parameter in method.ParameterList.Parameters)
				{
					var paramType = parameter.Type as QualifiedNameSyntax;
					var paramName = (paramType.Left as IdentifierNameSyntax).Identifier.Text;

					var model = new Model<ReturnType>();

					var componentNode = nodes.FindNode<StructDeclarationSyntax>(x => x.Identifier.Text == paramName);
					model.Set("compName".AsSpan(), Parameter.Create($"{componentNode.GetNamespace()}.{paramName}"));
					model.Set("compIdx".AsSpan(), Parameter.Create<float>(idx));

					models.Add(model);
					idx++;
				}

				// TODO: Only do first method for now
				break;
			}

			return models;
		}

		static List<Model<ReturnType>> GetMethods(ClassDeclarationSyntax node)
		{
			var models = new List<Model<ReturnType>>();

			foreach (var method in node.Members.Where(x => x is MethodDeclarationSyntax).Select(x => x as MethodDeclarationSyntax))
			{
				if (method.Identifier.Text != "Update" && !method.Identifier.Text.StartsWith("Update"))
					continue;

				var model = new Model<ReturnType>();

				var paramType = method.ParameterList.Parameters[0].Type as QualifiedNameSyntax;
				var name = paramType.Right as IdentifierNameSyntax;

				model.Set("methodName".AsSpan(), Parameter.Create(method.Identifier.Text));
				model.Set("methodType".AsSpan(), Parameter.Create(name.Identifier.Text == "Ref" ? "Single" : "Vector"));

				models.Add(model);
			}

			return models;
		}
	}
}
