using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace TemplateGenerator
{
	class EcsGenerator : ITemplateSourceGenerator<IdentifierNameSyntax>
	{
		public Guid Id { get; } = Guid.NewGuid();

		public string Template => "Ecs.tcs";

		public Model<ReturnType> CreateModel(Compilation compilation, IdentifierNameSyntax node)
		{
			var builderRoot = GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var buildStep = builderSteps.Single(x => x.Name.Identifier.Text == "Build");
			var worldStep = builderSteps.First(x => x.Name.Identifier.Text == "World");

			var builderLocation = buildStep.Name.GetLocation();
			var loc = builderLocation.GetMappedLineSpan();

			var model = new Model<ReturnType>();
			// Ecs Info
			model.Set("namespace".AsSpan(), Parameter.Create(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("name".AsSpan(), Parameter.Create(GetEcsName(node)));
			model.Set("worlds".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetWorlds(worldStep)));

			// Intercept info
			model.Set("path".AsSpan(), Parameter.Create(loc.Path));
			model.Set("line".AsSpan(), Parameter.Create<float>(loc.StartLinePosition.Line + 1));
			model.Set("character".AsSpan(), Parameter.Create<float>(loc.StartLinePosition.Character + 1));

			return model;
		}

		public bool Filter(GeneratorSyntaxContext context, IdentifierNameSyntax node)
		{
			return node.Identifier.Text == "EcsBuilder";
		}

		public string GetName(IdentifierNameSyntax node)
		{
			return GetEcsName(node);
		}

		public static ExpressionStatementSyntax GetBuilderRoot(SyntaxNode node)
		{
			if (node is ExpressionStatementSyntax e)
				return e;

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

		static List<Model<ReturnType>> GetWorlds(MemberAccessExpressionSyntax step)
		{
			List<Model<ReturnType>> models = new();

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

				var nameArg = invocation.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
				var nameToken = nameArg.Token.ValueText;

				var model = new Model<ReturnType>();
				model.Set("worldName".AsSpan(), Parameter.Create(nameToken));

				models.Add(model);
			}

			return models;
		}
	}
}
