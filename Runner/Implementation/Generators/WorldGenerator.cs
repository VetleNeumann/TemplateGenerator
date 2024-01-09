using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using TemplateGenerator;

namespace EnCS.Generator
{
	static class WorldGeneratorDiagnostics
	{
		public static readonly DiagnosticDescriptor TypeMustBeValidArchType = new("ECS006", "Type must be a valid arch type", "", "SystemGenerator", DiagnosticSeverity.Error, true);
	}

	class WorldGenerator : ITemplateSourceGenerator<IdentifierNameSyntax>
	{
		public Guid Id { get; } = Guid.NewGuid();

		public string Template => ResourceReader.GetResource("World.tcs");

		public bool TryCreateModel(Compilation compilation, IdentifierNameSyntax node, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			var builderRoot = EcsGenerator.GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var worldStep = builderSteps.First(x => x.Name.Identifier.Text == "World");
			var systemStep = builderSteps.First(x => x.Name.Identifier.Text == "System");
			var archTypeStep = builderSteps.First(x => x.Name.Identifier.Text == "ArchType");
			var resourceStep = builderSteps.First(x => x.Name.Identifier.Text == "Resource");

			bool resourceManagerSuccess = ArchTypeGenerator.TryGetResourceManagers(compilation, resourceStep, diagnostics, out List<ResourceManager> resourceManagers);

			var systems = GetSystems(systemStep);
			var discradDiagnostics = new List<Diagnostic>();
			var archTypeSuccess = ArchTypeGenerator.TryGetArchTypes(compilation, archTypeStep, resourceManagers, discradDiagnostics, out List<ArchType> archTypes);

			model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(node.GetNamespace()));
			model.Set("ecsName".AsSpan(), new Parameter<string>(EcsGenerator.GetEcsName(node)));

			var worldSuccess = TryGetWorlds(compilation, worldStep, systems, archTypes, resourceManagers, diagnostics, out List<World> worlds);
			model.Set("worlds".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(worlds.Select(x => x.GetModel())));

			return worldSuccess;
		}

		public bool Filter(IdentifierNameSyntax node)
		{
			return node.Identifier.Text == "EcsBuilder";
		}

		public string GetName(IdentifierNameSyntax node)
		{
			return $"{EcsGenerator.GetEcsName(node)}_World";
		}

		public static bool TryGetWorlds(Compilation compilation, MemberAccessExpressionSyntax step, List<SystemName> allSystems, List<ArchType> allArchTypes, List<ResourceManager> resourceManagers, List<Diagnostic> diagnostics, out List<World> worlds)
		{
			worlds = new List<World>();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			int i = 1;
			foreach (var statement in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (statement.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
					continue;

				if (memberAccess.Name is not GenericNameSyntax genericName)
					continue;

				if (genericName.Identifier.Text != "World")
					continue;

				if (invocation.ArgumentList.Arguments.Count == 0)
					continue;

				var nameArg = invocation.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
				var nameToken = nameArg.Token.ValueText;

				if (!TryGetWorldArchTypes(genericName, allArchTypes, diagnostics, out List<ArchType> worldArchTypes))
					continue;

				var worldSystems = GetWorldSystems(compilation, worldArchTypes, allSystems, resourceManagers, diagnostics);
				var worldResourceManagers = worldSystems.SelectMany(x => x.resourceManagers).Concat(worldArchTypes.SelectMany(x => x.resourceManagers)).GroupBy(x => x.name).Select(x => x.First()).ToList();

				worlds.Add(new World()
				{
					name = nameToken,
					archTypes = worldArchTypes,
					systems = worldSystems,
					resourceManagers = worldResourceManagers
				});

				i++;
			}

			return worlds.Count > 0;
		}

		public static bool TryGetWorldArchTypes(GenericNameSyntax name, List<ArchType> validArchTypes, List<Diagnostic> diagnostics, out List<ArchType> archTypes)
		{
			archTypes = new List<ArchType>();

			foreach (TypeSyntax comp in name.TypeArgumentList.Arguments)
			{
				if (comp is IdentifierNameSyntax ident)
				{
					if (!validArchTypes.Any(x => x.name == ident.Identifier.Text))
					{
						diagnostics.Add(Diagnostic.Create(WorldGeneratorDiagnostics.TypeMustBeValidArchType, comp.GetLocation(), ""));
						return false;
					}

					archTypes.Add(validArchTypes.First(x => x.name == ident.Identifier.Text));
				}
				else if (comp is QualifiedNameSyntax qual)
				{
					var right = qual.Right as IdentifierNameSyntax;

					if (!validArchTypes.Any(x => x.name == right.Identifier.Text))
					{
						diagnostics.Add(Diagnostic.Create(WorldGeneratorDiagnostics.TypeMustBeValidArchType, comp.GetLocation(), ""));
						return false;
					}

					archTypes.Add(validArchTypes.First(x => x.name == right.Identifier.Text));
				}
				else
				{
					diagnostics.Add(Diagnostic.Create(WorldGeneratorDiagnostics.TypeMustBeValidArchType, comp.GetLocation(), ""));
					return false;
				}
			}

			return true;
		}

		static List<WorldSystem> GetWorldSystems(Compilation compilation, List<ArchType> worldArchTypes, List<SystemName> allSystems, List<ResourceManager> resourceManagers, List<Diagnostic> diagnostics)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			var models = new List<WorldSystem>();

			var worldComponentNames = worldArchTypes.SelectMany(x => x.components).Select(x => x.name);
			var resourceComponentNames = worldArchTypes.SelectMany(x => x.resourceComponents).Select(x => $"{x.resourceManager.ns}.{x.resourceManager.name}.{x.resourceManager.inType}");

			var names = worldComponentNames.Concat(resourceComponentNames);

			foreach (SystemName system in allSystems)
			{
				var systemNode = nodes.FindNode<ClassDeclarationSyntax>(x => x.Identifier.Text == system.name);
				//SystemGenerator.TryGetComponents(compilation, systemNode, 0, resourceManagers, diagnostics, out List<MethodComponent> systemComps);
				SystemGenerator.TryGetSystem(compilation, systemNode, diagnostics, out System systemStruct);

				// Filter out all systems wich this world cannot support
				if (!systemStruct.groups.SelectMany(x => x.components).Select(x => x.name).All(names.Contains))
					continue;

				List<ContainerGroup> systemGroupCompatibleContainers = new List<ContainerGroup>();
				foreach (var systemGroup in systemStruct.groups)
				{
					systemGroupCompatibleContainers.Add(new ContainerGroup()
					{
						containers = GetComptaibleContainers(worldArchTypes, systemGroup.components)
					});
				}

				List<ContainerGroup> containerGroups = GetContainerCombinations(systemGroupCompatibleContainers.ToArray());

				var systemContainers = systemGroupCompatibleContainers.SelectMany(x => x.containers).GroupBy(x => x.name).Select(x => x.First()).ToList();
				var systemResourceManagers = systemContainers.SelectMany(x => x.components).Where(x => x.type == "Resource").Select(x => x.resourceManager).GroupBy(x => x.name).Select(x => x.First()).ToList();
				models.Add(new WorldSystem()
				{
					name = system.name,
					groups = containerGroups,
					containers = systemContainers.ToList(),
					resourceManagers = systemResourceManagers
				});
			}

			return models;
		}

		static List<ContainerGroup> GetContainerCombinations(Span<ContainerGroup> groups)
		{
			if (groups == null || groups.Length == 0)
				return new List<ContainerGroup>();

			var combinations = new List<ContainerGroup>();
			if (groups.Length == 1)
			{
				foreach (var container in groups[0].containers)
				{
					var containers = new List<Container>();
					containers.Add(container);

					combinations.Add(new ContainerGroup()
					{
						containers = containers
					});
				}
			}
			else
			{
				var nextCombinations = GetContainerCombinations(groups.Slice(1, groups.Length - 1));

				foreach (var container in groups[0].containers)
				{
					foreach (var nextContainer in nextCombinations)
					{
						var containers = new List<Container>();
						containers.Add(container);
						containers.AddRange(nextContainer.containers);

						combinations.Add(new ContainerGroup()
						{
							containers = containers
						});
					}
				}
			}

			return combinations;
		}

		static List<Container> GetComptaibleContainers(List<ArchType> worldArchTypes, List<MethodComponent> systemComps)
		{
			List<Container> models = new();

			foreach (var archType in worldArchTypes)
			{
				if (!IsArchTypeComatible(archType, systemComps))
					continue;

				var resourceManagers = systemComps.Where(x => x.type == "Resource").Select(x => x.resourceManager).GroupBy(x => x.name).Select(x => x.First()).ToList();

				models.Add(new Container()
				{
					name = archType.name,
					components = systemComps,
					resourceManagers = resourceManagers
				});
			}

			return models;
		}

		static bool IsArchTypeComatible(ArchType archType, List<MethodComponent> components)
		{
			foreach (var component in components)
			{
				string compResourceName = $"{component.resourceManager.ns}.{component.resourceManager.name}.{component.resourceManager.inType}";
				if (!archType.components.Any(x => x.name == component.name) && !archType.resourceComponents.Any(x => x.name == compResourceName))
					return false;
			}

			return true;
		}

		public static List<SystemName> GetSystems(MemberAccessExpressionSyntax step)
		{
			var systems = new List<SystemName>();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			foreach (var statement in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (statement.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
					continue;

				if (memberAccess.Name is not GenericNameSyntax genericName)
					continue;

				if (genericName.Identifier.Text != "System")
					continue;

				foreach (IdentifierNameSyntax comp in genericName.TypeArgumentList.Arguments)
				{
					systems.Add(new SystemName()
					{
						name = comp.Identifier.Text
					});
				}
			}

			return systems;
		}
	}

	struct World
	{
		public string name;
		public List<ArchType> archTypes;
		public List<WorldSystem> systems;
		public List<ResourceManager> resourceManagers;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("worldName".AsSpan(), Parameter.Create(name));
			model.Set("worldArchTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(archTypes.Select(x => x.GetModel())));
			model.Set("worldSystems".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(systems.Select(x => x.GetModel())));
			model.Set("worldResourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(resourceManagers.Select(x => x.GetModel())));

			return model;
		}
	}

	struct WorldSystem
	{
		public string name;
		public List<Container> containers;
		public List<ContainerGroup> groups;
		public List<ResourceManager> resourceManagers;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("systemName".AsSpan(), Parameter.Create(name));
			model.Set("systemContainers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(containers.Select(x => x.GetModel())));
			model.Set("systemGroups".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(groups.Select(x => x.GetModel())));
			model.Set("systemResourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(resourceManagers.Select(x => x.GetModel())));

			return model;
		}
	}

	struct SystemName
	{
		public string name;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("systemName".AsSpan(), Parameter.Create(name));

			return model;
		}
	}

	struct ContainerGroup
	{
		public List<Container> containers;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("groupContainers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(containers.Select(x => x.GetModel())));
			model.Set("groupResourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(containers.SelectMany(x => x.resourceManagers).GroupBy(x => x.name).Select(x => x.First()).Select(x => x.GetModel())));

			return model;
		}
	}

	struct Container
	{
		public string name;
		public List<MethodComponent> components;
		public List<ResourceManager> resourceManagers;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("containerName".AsSpan(), Parameter.Create(name));
			model.Set("containerComponents".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(components.Select(x => x.GetModel())));
			model.Set("containerResourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(resourceManagers.Select(x => x.GetModel())));

			return model;
		}
	}
}
