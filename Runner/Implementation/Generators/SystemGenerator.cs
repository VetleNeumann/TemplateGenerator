using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using TemplateGenerator;

namespace EnCS.Generator
{
	static class SystemGeneratorDiagnostics
	{
		public static readonly DiagnosticDescriptor SystemUpdateMethodsMustBeEqual = new("ECS004", "All system update methods within the group must be equal", "", "SystemGenerator", DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor MethodArgumentsMustBeConcistent = new("ECS005", "All system update methods must only use Vector or Single types", "", "SystemGenerator", DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor MethodArgumentMustBeComponentOrResourceOrContext = new("ECS007", "All system method arguments must be a valid component, resource or context parameter", "", "SystemGenerator", DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor MethodCannotBeEmpty = new("ECS008", "System update method cannot be empty", "", "SystemGenerator", DiagnosticSeverity.Warning, true);

		public static readonly DiagnosticDescriptor MethodCannotBeInMoreThanOneGroup = new("ECS009", "System update method cannot be in more than one group", "", "SystemGenerator", DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor MethodsWithinGroupMustHaveIdenticalChunk = new("ECS010", "Methods within a group must have identical chunk sizes", "", "SystemGenerator", DiagnosticSeverity.Error, true);
	}

	class SystemGenerator : ITemplateSourceGenerator<ClassDeclarationSyntax>
	{
		public string Template => ResourceReader.GetResource("System.tcs");

		public bool TryCreateModel(Compilation compilation, ClassDeclarationSyntax node, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), new Parameter<string>(node.GetNamespace()));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));

			bool systemSuccess = TryGetSystem(compilation, node, diagnostics, out System system);
			model.Set("resourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(system.resourceManagers.Select(x => x.GetModel())));
			model.Set("groups".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(system.groups.Select(x => x.GetModel())));
			model.Set("reversedGroups".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(system.groups.AsEnumerable().Reverse().Select(x => x.GetModel())));
			model.Set("contexts".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(system.contexts.Select(x => x.GetModel())));

			return systemSuccess;
		}

		public bool Filter(ClassDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					string name = attributeSyntax.Name.GetName();

					if (name == "SystemAttribute" || name == "System")
						return true;
				}
			}

			return false;
		}

		public string GetName(ClassDeclarationSyntax node)
		{
			return node.Identifier.ToString();
		}

		public static bool TryGetSystem(Compilation compilation, ClassDeclarationSyntax node, List<Diagnostic> diagnostics, out System system)
		{
			TryGetResourceManagers(compilation, node, diagnostics, out List<ResourceManager> resourceManagers);
			var uniqueResourceManagers = resourceManagers.GroupBy(x => x.name).Select(x => x.First());

			TryGetPrePostLoopMethods(node, diagnostics, out List<SystemMethod> preLoops, out List<SystemMethod> postLoops);
			TryGetSystemContexts(node, diagnostics, out List<SystemContext> contexts);

			bool methodSuccess = TryGetMethods(compilation, node, resourceManagers, contexts, diagnostics, out List<SystemMethod> methods);
			bool groupSuccess = TryGetGroups(methods, preLoops, postLoops, out List<SystemGroup> groups);

			system = new System()
			{
				groups = groups,
				resourceManagers = uniqueResourceManagers.ToList(),
				contexts = contexts
			};

			return methodSuccess && groupSuccess;
		}

		static bool TryGetSystemContexts(ClassDeclarationSyntax node, List<Diagnostic> diagnostics, out List<SystemContext> contexts)
		{
			var attribute = node.AttributeLists.SelectMany(x => x.Attributes).First(x => x.Name.GetName() == "System" || x.Name.GetName() == "SystemAttribute");
			contexts = new List<SystemContext>();

			if (attribute.Name is not GenericNameSyntax g)
				return true;

			foreach (TypeSyntax type in g.TypeArgumentList.Arguments)
			{
				if (type is not IdentifierNameSyntax i)
					continue;

				contexts.Add(new SystemContext()
				{
					type = i.Identifier.Text
				});
			}

			return true;
		}

		static bool TryGetPrePostLoopMethods(ClassDeclarationSyntax node, List<Diagnostic> diagnostics, out List<SystemMethod> preLoops, out List<SystemMethod> postLoops)
		{
			var systemPreLoops = node.Members.Where(x => x is MethodDeclarationSyntax m && IsMethodPreLoop(m)).Select(x => x as MethodDeclarationSyntax);
			preLoops = new List<SystemMethod>();
			foreach (var preLoop in systemPreLoops)
			{
				if (!TryGetMethodGroup(preLoop, out int group))
				{
					diagnostics.Add(Diagnostic.Create(SystemGeneratorDiagnostics.MethodCannotBeInMoreThanOneGroup, preLoop.GetLocation(), ""));
					continue;
				}

				preLoops.Add(new SystemMethod()
				{
					group = group,
					name = preLoop.Identifier.Text,
					type = "Unknown",
					chunk = 0,
					components = new List<MethodComponent>()
				});
			}

			var systemPostLoops = node.Members.Where(x => x is MethodDeclarationSyntax m && IsMethodPostLoop(m)).Select(x => x as MethodDeclarationSyntax);
			postLoops = new List<SystemMethod>();
			foreach (var postLoop in systemPostLoops)
			{
				if (!TryGetMethodGroup(postLoop, out int group))
				{
					diagnostics.Add(Diagnostic.Create(SystemGeneratorDiagnostics.MethodCannotBeInMoreThanOneGroup, postLoop.GetLocation(), ""));
					continue;
				}

				postLoops.Add(new SystemMethod()
				{
					group = group,
					name = postLoop.Identifier.Text,
					type = "Unknown",
					chunk = 0,
					components = new List<MethodComponent>()
				});
			}

			return true;
		}

		public static bool TryGetResourceManagers(Compilation compilation, ClassDeclarationSyntax node, List<Diagnostic> diagnostics, out List<ResourceManager> resourceManagers)
		{
			resourceManagers = new List<ResourceManager>();

			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());

			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if (attributeSyntax.Name is not GenericNameSyntax g)
						continue;

					if (g.Identifier.Text != "UsingResourceAttribute" && g.Identifier.Text != "UsingResource")
						continue;

					if (g.TypeArgumentList.Arguments.Count == 0)
						continue;

					var resourceManagerType = g.TypeArgumentList.Arguments[0] as IdentifierNameSyntax;
					var resourceManagerNode = nodes.FindNode<ClassDeclarationSyntax>(x => x.Identifier.Text == resourceManagerType.Identifier.Text);

					ResourceManagerGenerator.TryGetResourceManagers(compilation, resourceManagerNode, out List<ResourceManager> foundResourceManagers);
					resourceManagers.AddRange(foundResourceManagers);
				}
			}

			return resourceManagers.Count > 0;
		}

		static bool TryGetGroups(List<SystemMethod> methods, List<SystemMethod> preLoops, List<SystemMethod> postLoops, out List<SystemGroup> groups)
		{
			groups = new List<SystemGroup>();
			var groupedMethods = methods.GroupBy(x => x.group);

			foreach (var methodGroup in groupedMethods)
			{
				int group = methodGroup.First().group;
				int chunk = methodGroup.First().chunk;

				if (!methodGroup.All(x => x.chunk == chunk))
				{
					// TODO: Add error here
					continue;
				}

				groups.Add(new SystemGroup()
				{
					idx = group,
					chunk = chunk,
					components = methodGroup.First().components,
					methods = methodGroup.ToList(),
					preLoops = preLoops.Where(x => x.group == group).ToList(),
					postLoops = postLoops.Where(x => x.group == group).ToList()
				});
			}

			groups = groups.OrderBy(x => x.idx).ToList();
			return groups.Count > 0;
		}

		static bool TryGetComponents(Compilation compilation, ClassDeclarationSyntax node, int group, List<ResourceManager> resourceManagers, List<SystemContext> contexts, List<Diagnostic> diagnostics, out List<MethodComponent> components)
		{
			components = new List<MethodComponent>();

			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			var systemUpdateMethods = node.Members.Where(x => x is MethodDeclarationSyntax m && IsMethodSystemUpdate(m) && TryGetMethodGroup(m, out int g) && g == group).Select(x => x as MethodDeclarationSyntax);

			if (systemUpdateMethods.Count() == 0)
				return false;

			var firstMethod = systemUpdateMethods.First();

			if (!IsMethodArgumentsEqual(firstMethod, systemUpdateMethods))
			{
				diagnostics.Add(Diagnostic.Create(SystemGeneratorDiagnostics.SystemUpdateMethodsMustBeEqual, node.GetLocation(), ""));
				return false;
			}

			int idx = 1;
			foreach (var parameter in firstMethod.ParameterList.Parameters)
			{
				if (parameter.Type is QualifiedNameSyntax qualifiedType)
				{
					if (!TryGetNormalComponent(qualifiedType, nodes, idx, diagnostics, out MethodComponent component))
						continue;

					components.Add(component);
					idx++;
				}
				else if (parameter.Type is IdentifierNameSyntax identifierType) // Assume this is a resource or context parameter
				{
					bool resourceSuccess = TryGetResourceComponent(identifierType, resourceManagers, idx, diagnostics, out MethodComponent resourceComponent);
					bool contextSuccess = TryGetContextComponent(identifierType, contexts, out MethodComponent contextComponent);

					if (resourceSuccess)
					{
						components.Add(resourceComponent);
					}
					else if (contextSuccess)
					{
						components.Add(contextComponent);
						continue;
					}
					else
					{
						diagnostics.Add(Diagnostic.Create(SystemGeneratorDiagnostics.MethodArgumentMustBeComponentOrResourceOrContext, identifierType.GetLocation(), ""));
						continue;
					}

					idx++;
				}

			}

			return components.Count > 0;
		}

		static bool TryGetNormalComponent(QualifiedNameSyntax type, IEnumerable<SyntaxNode> nodes, int idx, List<Diagnostic> diagnostics, out MethodComponent component)
		{
			var paramName = (type.Left as IdentifierNameSyntax).Identifier.Text;
			var componentNode = nodes.FindNode<StructDeclarationSyntax>(x => x.Identifier.Text == paramName);

			if (!ComponentGenerator.IsValidComponent(componentNode, diagnostics))
			{
				component = default;
				return false;
			}

			component = new MethodComponent()
			{
				name = $"{componentNode.GetNamespace()}.{paramName}",
				idx = idx,
				type = "Component"
			};

			return true;
		}

		static bool TryGetResourceComponent(IdentifierNameSyntax type, List<ResourceManager> resourceManagers, int idx, List<Diagnostic> diagnostics, out MethodComponent component)
		{
			if (!resourceManagers.Any(x => x.outType == type.Identifier.Text))
			{
				component = default;
				return false;
			}

			var resourceManager = resourceManagers.First(x => x.outType == type.Identifier.Text);

			component = new MethodComponent()
			{
				name = $"{resourceManager.ns}.{resourceManager.name}.{resourceManager.inType}",
				idx = idx,
				type = "Resource",
				resourceManager = resourceManager
			};

			return true;
		}

		static bool TryGetContextComponent(IdentifierNameSyntax type, List<SystemContext> contexts, out MethodComponent component)
		{
			if (!contexts.Any(x => x.type == type.Identifier.Text))
			{
				component = default;
				return false;
			}

			var contextComponent = contexts.First(x => x.type == type.Identifier.Text);
			component = new MethodComponent()
			{
				name = contextComponent.type,
				idx = 0,
				type = "Context"
			};

			return true;
		}

		static bool IsMethodArgumentsEqual(MethodDeclarationSyntax method, IEnumerable<MethodDeclarationSyntax> methods)
		{
			foreach (var item in methods)
			{
				for (int i = 0; i < method.ParameterList.Parameters.Count; i++)
				{
					// Non qualidfied names are assumed to be resources, and they are not equal to vectorized versions.
					if (method.ParameterList.Parameters[i].Type is not QualifiedNameSyntax)
						continue;

					if (method.ParameterList.Parameters[i].Identifier.Text != item.ParameterList.Parameters[i].Identifier.Text)
						return false;
				}
			}

			return true;
		}

		public static bool TryGetMethods(Compilation compilation, ClassDeclarationSyntax node, List<ResourceManager> resourceManagers, List<SystemContext> contexts, List<Diagnostic> diagnostics, out List<SystemMethod> methods)
		{
			methods = new List<SystemMethod>();

			foreach (var method in node.Members.Where(x => x is MethodDeclarationSyntax).Select(x => x as MethodDeclarationSyntax))
			{
				if (!IsMethodSystemUpdate(method, diagnostics))
					continue;

				if (!TryGetMethodGroup(method, out int group))
				{
					diagnostics.Add(Diagnostic.Create(SystemGeneratorDiagnostics.MethodCannotBeInMoreThanOneGroup, method.GetLocation(), ""));
					continue;
				}

				if (!TryGetMethodChunk(method, out int chunk))
					continue;

				if (!TryGetComponents(compilation, node, group, resourceManagers, contexts, diagnostics, out List<MethodComponent> components))
					continue;

				methods.Add(new SystemMethod()
				{
					name = method.Identifier.Text,
					type = GetMethodType(method),
					group = group,
					chunk = chunk,
					components = components
				});
			}

			return methods.Count > 0;
		}

		static bool TryGetMethodGroup(MethodDeclarationSyntax method, out int group)
		{
			var attributes = method.AttributeLists.SelectMany(x => x.Attributes);
			var groupAttribute = attributes.Where(x => x.Name is IdentifierNameSyntax g && (g.Identifier.Text == "SystemLayer" || g.Identifier.Text == "SystemLayerAttribute"));

			group = 0;
			if (groupAttribute.Count() > 1)
				return false;

			if (groupAttribute.Count() == 1)
				group = int.Parse(groupAttribute.Single().ArgumentList.Arguments[0].ToString());

			return true;
		}

		static bool TryGetMethodChunk(MethodDeclarationSyntax method, out int chunk)
		{
			var attributes = method.AttributeLists.SelectMany(x => x.Attributes);
			var groupAttribute = attributes.Where(x => x.Name is IdentifierNameSyntax g && (g.Identifier.Text == "SystemLayer" || g.Identifier.Text == "SystemLayerAttribute"));

			chunk = 0;
			if (groupAttribute.Count() > 1)
				return false;

			if (groupAttribute.Count() == 1)
			{
				var args = groupAttribute.Single().ArgumentList.Arguments;

				if (args.Count > 1)
					chunk = int.Parse(args[1].ToString());
			}

			return true;
		}

		static bool IsMethodSystemUpdate(MethodDeclarationSyntax method, List<Diagnostic> diagnostics)
		{
			if (!IsMethodSystemUpdate(method))
				return false;

			if (method.ParameterList.Parameters.Count == 0)
			{
				diagnostics.Add(Diagnostic.Create(SystemGeneratorDiagnostics.MethodCannotBeEmpty, method.GetLocation(), ""));
				return false;
			}

			if (!IsMethodArgumentsConsistent(method))
			{
				diagnostics.Add(Diagnostic.Create(SystemGeneratorDiagnostics.MethodArgumentsMustBeConcistent, method.GetLocation(), ""));
				return false;
			}

			return true;
		}

		static bool IsMethodSystemUpdate(MethodDeclarationSyntax method)
		{
			var attributes = method.AttributeLists.SelectMany(x => x.Attributes);
			if (!attributes.Any(x => x.Name is IdentifierNameSyntax i && (i.Identifier.Text == "SystemUpdate" || i.Identifier.Text == "SystemUpdateAttribute")))
				return false;

			return true;
		}

		static bool IsMethodPreLoop(MethodDeclarationSyntax method)
		{
			var attributes = method.AttributeLists.SelectMany(x => x.Attributes);
			if (!attributes.Any(x => x.Name is IdentifierNameSyntax i && (i.Identifier.Text == "SystemPreLoop" || i.Identifier.Text == "SystemPreLoopAttribute")))
				return false;

			return true;
		}

		static bool IsMethodPostLoop(MethodDeclarationSyntax method)
		{
			var attributes = method.AttributeLists.SelectMany(x => x.Attributes);
			if (!attributes.Any(x => x.Name is IdentifierNameSyntax i && (i.Identifier.Text == "SystemPostLoop" || i.Identifier.Text == "SystemPostLoopAttribute")))
				return false;

			return true;
		}

		static bool IsMethodArgumentsConsistent(MethodDeclarationSyntax method)
		{
			var args = method.ParameterList.Parameters.Where(x => x.Type is QualifiedNameSyntax);

			var firstParamType = args.First();
			var firstName = (firstParamType.Type as QualifiedNameSyntax).Right as IdentifierNameSyntax;

			foreach (var item in args)
			{
				if (item.Type is not QualifiedNameSyntax paramType)
					continue;

				var name = paramType.Right as IdentifierNameSyntax;

				if (name.Identifier.Text != firstName.Identifier.Text)
					return false;
			}

			return true;
		}

		static string GetMethodType(MethodDeclarationSyntax method)
		{
			var args = method.ParameterList.Parameters.Where(x => x.Type is QualifiedNameSyntax);

			var firstParamType = args.First();
			var type = (firstParamType.Type as QualifiedNameSyntax).Right as IdentifierNameSyntax;

			return type.Identifier.Text == "Ref" ? "Single" : "Vector";
		}
	}

	struct System
	{
		public List<ResourceManager> resourceManagers;
		public List<SystemGroup> groups;
		public List<SystemContext> contexts;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("systemResourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(resourceManagers.Select(x => x.GetModel())));
			model.Set("systemGroups".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(groups.Select(x => x.GetModel())));
			model.Set("systemReversedGroups".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(groups.AsEnumerable().Reverse().Select(x => x.GetModel())));
			model.Set("systemContexts".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(contexts.Select(x => x.GetModel())));

			return model;
		}
	}

	struct SystemMethod
	{
		public string name;
		public string type;
		public int group;
		public int chunk;
		public List<MethodComponent> components;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("methodName".AsSpan(), Parameter.Create(name));
			model.Set("methodType".AsSpan(), Parameter.Create(type));
			model.Set("methodGroup".AsSpan(), Parameter.Create<float>(group));
			model.Set("methodChunk".AsSpan(), Parameter.Create<float>(chunk));
			model.Set("methodComponents".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(components.Select(x => x.GetModel())));

			return model;
		}
	}

	struct SystemGroup
	{
		public int idx;
		public int chunk;
		public List<MethodComponent> components;
		public List<SystemMethod> methods;
		public List<SystemMethod> preLoops;
		public List<SystemMethod> postLoops;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("groupIdx".AsSpan(), Parameter.Create<float>(idx));
			model.Set("groupChunk".AsSpan(), Parameter.Create<float>(chunk));
			model.Set("groupComponents".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(components.Select(x => x.GetModel())));
			model.Set("groupMethods".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(methods.Select(x => x.GetModel())));
			model.Set("groupPreLoops".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(preLoops.Select(x => x.GetModel())));
			model.Set("groupPostLoops".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(postLoops.Select(x => x.GetModel())));

			List<IModel<ReturnType>> modelList = new List<IModel<ReturnType>>();
			if (chunk != 0 && (preLoops.Count > 0 || postLoops.Count > 0))
				modelList.Add(new Model<ReturnType>());

			model.Set("groupHasPreOrPostLoop".AsSpan(), Parameter.CreateEnum(modelList));

			return model;
		}
	}

	struct MethodComponent
	{
		public string name;
		public int idx;
		public string type;
		public ResourceManager resourceManager;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("compName".AsSpan(), Parameter.Create(name));
			model.Set("compIdx".AsSpan(), Parameter.Create<float>(idx));
			model.Set("compType".AsSpan(), Parameter.Create(type));
			model.Set("compResourceManager".AsSpan(), Parameter.Create((IModel<ReturnType>)resourceManager.GetModel()));

			return model;
		}
	}

	struct SystemContext
	{
		public string type;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("contextType".AsSpan(), Parameter.Create(type));

			return model;
		}
	}
}
