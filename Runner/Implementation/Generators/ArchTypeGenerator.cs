using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;
using TemplateGenerator;

namespace EnCS.Generator
{
	static class ArchTypeGeneratorDiagnostics
	{
		public static readonly DiagnosticDescriptor ArchTypeMustBeValidComponent = new("ECS003", "Archtype can only contain valid components", "Archtype member is not valid component", "ArchTypeGenerator", DiagnosticSeverity.Error, true);
	}

	class ArchTypeGenerator : ITemplateSourceGenerator<IdentifierNameSyntax>
	{
		public Guid Id { get; } = Guid.NewGuid();

		public string Template => ResourceReader.GetResource("ArchType.tcs");

        public bool TryCreateModel(Compilation compilation, IdentifierNameSyntax node, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			var builderRoot = EcsGenerator.GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var archTypeStep = builderSteps.First(x => x.Name.Identifier.Text == "ArchType");
			var resourceStep = builderSteps.First(x => x.Name.Identifier.Text == "Resource");

			bool resourceManagerSuccess = TryGetResourceManagers(compilation, resourceStep, diagnostics, out List<ResourceManager> resourceManagers);

			model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(node.GetNamespace()));
			model.Set("ecsName".AsSpan(), new Parameter<string>(EcsGenerator.GetEcsName(node)));

			var archTypeSuccess = TryGetArchTypes(compilation, archTypeStep, resourceManagers, diagnostics, out List<ArchType> archTypes);
			model.Set("archTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(archTypes.Select(x => x.GetModel())));

			return resourceManagerSuccess && archTypeSuccess;
		}

		public bool Filter(IdentifierNameSyntax node)
		{
			return node.Identifier.Text == "EcsBuilder";
		}

		public string GetName(IdentifierNameSyntax node)
		{
			return $"{EcsGenerator.GetEcsName(node)}_ArchType";
		}

		public static bool TryGetResourceManagers(Compilation compilation, MemberAccessExpressionSyntax step, List<Diagnostic> diagnostics, out List<ResourceManager> resourceManagers)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			resourceManagers = new List<ResourceManager>();

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

				if (genericName.Identifier.Text != "ResourceManager")
					continue;

				var resourceManagerType = genericName.TypeArgumentList.Arguments[0] as IdentifierNameSyntax;
				var resourceManager = nodes.FindNode<ClassDeclarationSyntax>(x => x.Identifier.Text == resourceManagerType.Identifier.Text);

				ResourceManagerGenerator.TryGetResourceManagers(compilation, resourceManager, out List<ResourceManager> localResourceManagers);
				resourceManagers.AddRange(localResourceManagers);
			}

			return true;
		}

		public static bool TryGetArchTypes(Compilation compilation, MemberAccessExpressionSyntax step, List<ResourceManager> resourceManagers, List<Diagnostic> diagnostics, out List<ArchType> models)
		{
			models = new List<ArchType>();

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

				if (genericName.Identifier.Text != "ArchType")
					continue;

				var nameArg = invocation.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
				var nameToken = nameArg.Token.ValueText;

				bool compSuccess = TryGetComponents(compilation, genericName, resourceManagers, diagnostics, out List<Component> components);
				bool resourceCompSuccess = TryGetResourceComponents(compilation, genericName, resourceManagers, out List<ResourceComponent> resourceComponents);

				if (!compSuccess && !resourceCompSuccess)
					continue;

				var uniqueResourcManagers = resourceComponents.Select(x => x.resourceManager).GroupBy(x => x.name).Select(x => x.First()).ToList();

				models.Add(new ArchType()
				{
					name = nameToken,
					components = components,
					resourceComponents = resourceComponents,
					resourceManagers = uniqueResourcManagers
				});
			}

			return models.Count > 0;
		}

		static bool TryGetComponents(Compilation compilation, GenericNameSyntax name, List<ResourceManager> resourceManagers, List<Diagnostic> diagnostics, out List<Component> models)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			models = new List<Component>();

			foreach (IdentifierNameSyntax comp in name.TypeArgumentList.Arguments)
			{
				var compNode = nodes.FindNode<StructDeclarationSyntax>(x => x.Identifier.Text == comp.Identifier.Text);

				var discardDiagnostics = new List<Diagnostic>();
				if (!ComponentGenerator.IsValidComponent(compNode, discardDiagnostics))
				{
					// Only show error if struct is not valid component and not a registerd resource.
					if (!IsResource(compNode, resourceManagers))
						diagnostics.Add(Diagnostic.Create(ArchTypeGeneratorDiagnostics.ArchTypeMustBeValidComponent, comp.GetLocation(), ""));

					continue;
				}	

				models.Add(new Component()
				{
					name = $"{compNode.GetNamespace()}.{comp.Identifier.Text}",
					varName = comp.Identifier.Text
				});
			}

			return models.Count > 0;
		}

		static bool TryGetResourceComponents(Compilation compilation, GenericNameSyntax name, List<ResourceManager> resourceManagers, out List<ResourceComponent> models)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			models = new List<ResourceComponent>();

			foreach (IdentifierNameSyntax comp in name.TypeArgumentList.Arguments)
			{
				var compNode = nodes.FindNode<StructDeclarationSyntax>(x => x.Identifier.Text == comp.Identifier.Text);

				if (!TryGetResourceManager(compNode, resourceManagers, out ResourceManager resourceManager))
					continue;

				models.Add(new ResourceComponent()
				{
					name = $"{resourceManager.ns}.{resourceManager.name}.{comp.Identifier.Text}",
					varName = comp.Identifier.Text,
					resourceManager = resourceManager
				});
			}

			return models.Count > 0;
		}

		static bool IsResource(StructDeclarationSyntax comp, List<ResourceManager> resourceManagers)
		{
			return resourceManagers.Any(x => x.inType == comp.Identifier.Text);
		}

		static bool TryGetResourceManager(StructDeclarationSyntax comp, List<ResourceManager> resourceManagers, out ResourceManager resourceManager)
		{
			resourceManager = resourceManagers.FirstOrDefault(x => x.inType == comp.Identifier.Text);
			return IsResource(comp, resourceManagers);
		}
	}

	struct ArchType
	{
		public string name;
		public List<Component> components;
		public List<ResourceComponent> resourceComponents;
		public List<ResourceManager> resourceManagers;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("archTypeName".AsSpan(), Parameter.Create(name));
			model.Set("archTypeComponents".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(components.Select(x => x.GetModel()).Concat(resourceComponents.Select(x => x.GetModel()))));
			model.Set("archTypeResourceComponents".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(resourceComponents.Select(x => x.GetModel())));
			model.Set("archTypeResourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(resourceManagers.Select(x => x.GetModel())));

			return model;
		}
	}

	struct Component
	{
		public string name;
		public string varName;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("compName".AsSpan(), Parameter.Create(name));
			model.Set("compVarName".AsSpan(), Parameter.Create(varName));
			model.Set("compType".AsSpan(), Parameter.Create("Component"));
				
			return model;
		}
	}

	struct ResourceComponent
	{
		public string name;
		public string varName;
		public ResourceManager resourceManager;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("compName".AsSpan(), Parameter.Create(name));
			model.Set("compVarName".AsSpan(), Parameter.Create(varName));
			model.Set("compResourceManager".AsSpan(), Parameter.Create((IModel<ReturnType>)resourceManager.GetModel()));
			model.Set("compType".AsSpan(), Parameter.Create("Resource"));

			return model;
		}
	}
}
