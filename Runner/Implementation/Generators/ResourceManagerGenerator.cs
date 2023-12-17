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

			bool resourceManagerSuccess = TryGetResourceManagers(node, out List<ResourceManager> resourceManagers);
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

		public static bool TryGetResourceManagers(ClassDeclarationSyntax resourceManager, out List<ResourceManager> resourceManagers)
		{
			resourceManagers = new List<ResourceManager>();

			var managerTypes = resourceManager.BaseList.Types.Where(x => x is SimpleBaseTypeSyntax s && s.Type is GenericNameSyntax g && g.Identifier.Text == "IResourceManager");
			foreach (var manager in managerTypes)
			{
				if (manager is not SimpleBaseTypeSyntax s)
					continue;

				if (s.Type is not GenericNameSyntax g)
					continue;

				resourceManagers.Add(new ResourceManager()
				{
					name = $"{resourceManager.GetNamespace()}.{resourceManager.Identifier.Text}",
					type = g.TypeArgumentList.Arguments[0].ToString()
				});
			}

			return resourceManagers.Count > 0;
		}
	}

	struct ResourceManager
	{
		public string name;
		public string type;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("resourceManagerName".AsSpan(), Parameter.Create(name));
			model.Set("resourceManagerType".AsSpan(), Parameter.Create(type));

			return model;
		}
	}
}
