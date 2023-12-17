using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TemplateGenerator;

namespace EnCS.Generator
{
	class SystemGenerator : ITemplateSourceGenerator<ClassDeclarationSyntax>
	{
		public string Template => ResourceReader.GetResource("System.tcs");

		public bool TryCreateModel(Compilation compilation, ClassDeclarationSyntax node, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), new Parameter<string>(node.GetNamespace()));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));

			var components = GetComponents(compilation, node);
			model.Set("components".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(components.Select(x => x.GetModel())));

			var methods = GetMethods(node);
			model.Set("methods".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(methods.Select(x => x.GetModel())));

			return true;
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

		static List<SystemComponent> GetComponents(Compilation compilation, ClassDeclarationSyntax node)
		{
			var models = new List<SystemComponent>();

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
					var componentNode = nodes.FindNode<StructDeclarationSyntax>(x => x.Identifier.Text == paramName);

					models.Add(new SystemComponent()
					{
						name = $"{componentNode.GetNamespace()}.{paramName}",
						idx = idx,
					});

					idx++;
				}

				// TODO: Only do first method for now
				break;
			}

			return models;
		}

		static List<SystemMethod> GetMethods(ClassDeclarationSyntax node)
		{
			var models = new List<SystemMethod>();

			foreach (var method in node.Members.Where(x => x is MethodDeclarationSyntax).Select(x => x as MethodDeclarationSyntax))
			{
				if (method.Identifier.Text != "Update" && !method.Identifier.Text.StartsWith("Update"))
					continue;

				var paramType = method.ParameterList.Parameters[0].Type as QualifiedNameSyntax;
				var name = paramType.Right as IdentifierNameSyntax;

				models.Add(new SystemMethod()
				{
					name = method.Identifier.Text,
					type = name.Identifier.Text == "Ref" ? "Single" : "Vector"
				});
			}

			return models;
		}
	}

	struct SystemMethod
	{
		public string name;
		public string type;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("methodName".AsSpan(), Parameter.Create(name));
			model.Set("methodType".AsSpan(), Parameter.Create(type));

			return model;
		}
	}

	struct SystemComponent
	{
		public string name;
		public int idx;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("compName".AsSpan(), Parameter.Create(name));
			model.Set("compIdx".AsSpan(), Parameter.Create<float>(idx));

			return model;
		}
	}
}
