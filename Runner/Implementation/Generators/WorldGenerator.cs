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

			var worlds = GetWorlds(compilation, worldStep, systems, archTypes);
			model.Set("worlds".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(worlds.Select(x => x.GetModel())));

			return true;
		}

		public bool Filter(IdentifierNameSyntax node)
		{
			return node.Identifier.Text == "EcsBuilder";
		}

		public string GetName(IdentifierNameSyntax node)
		{
			return $"{EcsGenerator.GetEcsName(node)}_World";
		}

		static List<World> GetWorlds(Compilation compilation, MemberAccessExpressionSyntax step, List<SystemName> allSystems, List<ArchType> allArchTypes)
		{
			var models = new List<World>();

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

				var worldArchTypes = GetWorldArchTypes(genericName);
				var worldSystems = GetWorldSystems(compilation, worldArchTypes, allSystems, allArchTypes);

				models.Add(new World()
				{
					name = nameToken,
					archTypes = worldArchTypes,
					systems = worldSystems
				});

				i++;
			}

			return models;
		}

		public static List<ArchTypeName> GetWorldArchTypes(GenericNameSyntax name)
		{
			var archTypes = new List<ArchTypeName>();

			foreach (TypeSyntax comp in name.TypeArgumentList.Arguments)
			{
				if (comp is IdentifierNameSyntax ident)
				{
					archTypes.Add(new ArchTypeName()
					{
						name = ident.Identifier.Text
					});
					continue;
				}
				else if (comp is QualifiedNameSyntax qual)
				{
					var right = qual.Right as IdentifierNameSyntax;
					archTypes.Add(new ArchTypeName()
					{
						name = right.Identifier.Text
					});
					continue;
				}
			}

			return archTypes;
		}

		static List<System> GetWorldSystems(Compilation compilation, List<ArchTypeName> worldArchTypes, List<SystemName> allSystems, List<ArchType> allArchTypes)
		{
			var models = new List<System>();

			var worldComponents = worldArchTypes.SelectMany(x => allArchTypes.First(y => y.name == x.name).components).ToList();
			var worldComponentNames = worldComponents.Select(x => x.name);

			foreach (SystemName system in allSystems)
			{
				var systemComps = GetSystemComponents(compilation, system);

				// Filter out all systems wich this world cannot support
				if (!systemComps.Select(x => x.name).All(worldComponentNames.Contains))
					continue;

				models.Add(new System()
				{
					name = system.name,
					containers = GetComptaibleContainers(worldArchTypes, systemComps, allArchTypes)
				});
			}

			return models;
		}

		static List<Container> GetComptaibleContainers(List<ArchTypeName> worldArchTypes, List<ComponentName> systemComps, List<ArchType> allArchTypes)
		{
			List<Container> models = new();

			foreach (var archTypeName in worldArchTypes)
			{
				var components = allArchTypes.First(x => x.name == archTypeName.name).components;
				if (!systemComps.All(x => components.Any(y => y.name == x.name)))
					continue;

				models.Add(new Container()
				{
					name = archTypeName.name,
					components = systemComps
				});
			}

			return models;
		}

		static List<ComponentName> GetSystemComponents(Compilation compilation, SystemName system)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			var systemNode = nodes.FindNode<ClassDeclarationSyntax>(x => x.Identifier.Text == system.name);

			List<ComponentName> names = new List<ComponentName>();
			foreach (var method in systemNode.Members.Where(x => x is MethodDeclarationSyntax).Select(x => x as MethodDeclarationSyntax))
			{
				if (method.Identifier.Text != "Update")
					continue;

				foreach (var parameter in method.ParameterList.Parameters)
				{
					var paramType = parameter.Type as QualifiedNameSyntax;
					string compName = (paramType.Left as IdentifierNameSyntax).Identifier.Text;

					var compNode = nodes.FindNode<StructDeclarationSyntax>(x => x.Identifier.Text == compName);

					names.Add(new ComponentName()
					{
						name = $"{compNode.GetNamespace()}.{compName}"
					});
				}

				// TODO: Only do first method for now
				break;
			}

			return names;
		}

		static List<SystemName> GetSystems(MemberAccessExpressionSyntax step)
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
		public List<ArchTypeName> archTypes;
		public List<System> systems;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("worldName".AsSpan(), Parameter.Create(name));
			model.Set("worldArchTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(archTypes.Select(x => x.GetModel())));

			if (systems != null)
				model.Set("worldSystems".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(systems.Select(x => x.GetModel())));

			return model;
		}
	}

	struct ArchTypeName
	{
		public string name;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("archTypeName".AsSpan(), Parameter.Create(name));

			return model;
		}
	}

	struct System
	{
		public string name;
		public List<Container> containers;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("systemName".AsSpan(), Parameter.Create(name));
			model.Set("systemContainers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(containers.Select(x => x.GetModel())));

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

	struct Container
	{
		public string name;
		public List<ComponentName> components;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("containerName".AsSpan(), Parameter.Create(name));
			model.Set("containerComponents".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(components.Select(x => x.GetModel())));

			return model;
		}
	}

	struct ComponentName
	{
		public string name;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("compName".AsSpan(), Parameter.Create(name));

			return model;
		}
	}
}
