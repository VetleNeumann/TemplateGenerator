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
	class ResourceManagerGenerator : ITemplateSourceGenerator<ClassDeclarationSyntax>
	{
		public string Template => ResourceReader.GetResource("ResourceManager.tcs");

		public bool TryCreateModel(Compilation compilation, ClassDeclarationSyntax node, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), new Parameter<string>(node.GetNamespace()));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));

			bool resourceManagerSuccess = TryGetResourceManagers(compilation, node, out List<ResourceManager> resourceManagers);
			model.Set("resourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(resourceManagers.Select(x => x.GetModel())));

			return resourceManagerSuccess;
		}

		public bool Filter(ClassDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if ((attributeSyntax.Name as SimpleNameSyntax).Identifier.Text == "ResourceManagerAttribute")
						return true;

					if ((attributeSyntax.Name as SimpleNameSyntax).Identifier.Text == "ResourceManager")
						return true;
				}
			}

			return false;
		}

		public string GetName(ClassDeclarationSyntax node)
		{
			return node.Identifier.ToString();
		}

		public static bool TryGetResourceManagers(Compilation compilation, ClassDeclarationSyntax resourceManager, out List<ResourceManager> resourceManagers)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			resourceManagers = new List<ResourceManager>();

			var managerTypes = resourceManager.BaseList.Types.Where(x => x is SimpleBaseTypeSyntax s && s.Type is GenericNameSyntax g && g.Identifier.Text == "IResourceManager");
			foreach (var manager in managerTypes)
			{
				if (manager is not SimpleBaseTypeSyntax s)
					continue;

				if (s.Type is not GenericNameSyntax g)
					continue;

				string inType;
				string outType;

				if (g.TypeArgumentList.Arguments.Count == 1)
				{
					inType = g.TypeArgumentList.Arguments[0].ToString();
					outType = inType;
				}
				else if (g.TypeArgumentList.Arguments.Count == 2)
				{
					inType = g.TypeArgumentList.Arguments[0].ToString();
					outType = g.TypeArgumentList.Arguments[1].ToString();
				}
				else
				{
					throw new Exception("Invalid number of type arguments for resource managers, must be 1 or 2");
				}

				var typeName = g.TypeArgumentList.Arguments[0].ToString();
				var typeNode = nodes.FindNode<StructDeclarationSyntax>(x => x.Identifier.Text == typeName);

				resourceManagers.Add(new ResourceManager()
				{
					name = resourceManager.Identifier.Text,
					ns = resourceManager.GetNamespace(),
					//type = typeName,
					inType = inType,
					outType = outType,
					typeNs = typeNode.GetNamespace()
				});
			}

			return resourceManagers.Count > 0;
		}
	}

	struct ResourceManager
	{
		public string name;
		public string ns;
		//public string type;
		public string inType;
		public string outType;
		public string typeNs;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("resourceManagerName".AsSpan(), Parameter.Create(name));
			model.Set("resourceManagerNamespace".AsSpan(), Parameter.Create(ns));
			//model.Set("resourceManagerType".AsSpan(), Parameter.Create(type));
			model.Set("resourceManagerInType".AsSpan(), Parameter.Create(inType));
			model.Set("resourceManagerOutType".AsSpan(), Parameter.Create(outType));
			model.Set("resourceManagerTypeNamespace".AsSpan(), Parameter.Create(typeNs));

			return model;
		}
	}
}
