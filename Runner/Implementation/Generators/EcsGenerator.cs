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
	class EcsGenerator : ITemplateSourceGenerator<IdentifierNameSyntax>
	{
		public string Template => ResourceReader.GetResource("Ecs.tcs");

		public bool TryCreateModel(Compilation compilation, IdentifierNameSyntax node, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			var builderRoot = GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var buildStep = builderSteps.Single(x => x.Name.Identifier.Text == "Build");
			var worldStep = builderSteps.First(x => x.Name.Identifier.Text == "World");
			var archTypeStep = builderSteps.First(x => x.Name.Identifier.Text == "ArchType");
			var resourceStep = builderSteps.First(x => x.Name.Identifier.Text == "Resource");

			bool resourceManagerSuccess = ArchTypeGenerator.TryGetResourceManagers(compilation, resourceStep, diagnostics, out List<ResourceManager> resourceManagers);

			model = new Model<ReturnType>();

			// Ecs Info
			model.Set("namespace".AsSpan(), Parameter.Create(node.GetNamespace()));
			model.Set("name".AsSpan(), Parameter.Create(GetEcsName(node)));

			var worlds = GetWorlds(worldStep);
			model.Set("worlds".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(worlds.Select(x => x.GetModel())));

			var discradDiagnostics = new List<Diagnostic>();
			var archTypeSuccess = ArchTypeGenerator.TryGetArchTypes(compilation, archTypeStep, resourceManagers, discradDiagnostics, out List<ArchType> archTypes);
			model.Set("archTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(archTypes.Select(x => x.GetModel())));

			// Intercept info
			var builderLocation = buildStep.Name.GetLocation();
			var loc = builderLocation.GetMappedLineSpan();

			model.Set("path".AsSpan(), Parameter.Create(loc.Path));
			model.Set("line".AsSpan(), Parameter.Create<float>(loc.StartLinePosition.Line + 1));
			model.Set("character".AsSpan(), Parameter.Create<float>(loc.StartLinePosition.Character + 1));

			return true;
		}

		public bool Filter(IdentifierNameSyntax node)
		{
			return node.Identifier.Text == "EcsBuilder";
		}

		public string GetName(IdentifierNameSyntax node)
		{
			return GetEcsName(node);
		}

		public static SyntaxNode GetBuilderRoot(SyntaxNode node)
		{
			if (node is StatementSyntax)
				return node;

			return GetBuilderRoot(node.Parent);
		}

		public static string GetEcsName(IdentifierNameSyntax node)
		{
			var builderRoot = GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var buildStep = builderSteps.Single(x => x.Name.Identifier.Text == "Build");
			var genricName = buildStep.Name as GenericNameSyntax;

			return genricName.TypeArgumentList.Arguments[0].ToString();
		}

		static List<World> GetWorlds(MemberAccessExpressionSyntax step)
		{
			List<World> models = new();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			foreach (var system in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (system.Expression is not InvocationExpressionSyntax invocation)
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

				var archTypes = WorldGenerator.GetWorldArchTypes(genericName);

				models.Add(new World()
				{
					name = nameToken,
					archTypes = archTypes
				});
			}

			return models;
		}
	}
}
