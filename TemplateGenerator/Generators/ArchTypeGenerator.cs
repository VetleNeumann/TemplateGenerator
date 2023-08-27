using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateGenerator
{
	class ArchTypeGenerator : ITemplateSourceGenerator<IdentifierNameSyntax>
	{
		public Guid Id { get; } = Guid.NewGuid();

		public string Template => "ArchType.tcs";

        public Model<ReturnType> CreateModel(Compilation compilation, IdentifierNameSyntax node)
		{
			var builderRoot = EcsGenerator.GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var archTypeStep = builderSteps.First(x => x.Name.Identifier.Text == "ArchType");

			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("ecsName".AsSpan(), new Parameter<string>("Ecs"));
			model.Set("archTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetArchTypes(archTypeStep)));

			return model;
		}

		public bool Filter(GeneratorSyntaxContext context, IdentifierNameSyntax node)
		{
			return node.Identifier.Text == "EcsBuilder";
		}

		public string GetName(IdentifierNameSyntax node)
		{
			return $"{EcsGenerator.GetEcsName(node)}_ArchType";
		}

		static List<Model<ReturnType>> GetArchTypes(MemberAccessExpressionSyntax step)
		{
			var models = new List<Model<ReturnType>>();

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

				var model = new Model<ReturnType>();
				model.Set("archTypeName".AsSpan(), Parameter.Create($"ArchType{i}"));
				model.Set("components".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetComponents(genericName)));

				models.Add(model);

				i++;
			}

			return models;
		}

		static Model<ReturnType>[] GetComponents(GenericNameSyntax name)
		{
			var models = new List<Model<ReturnType>>();

			foreach (IdentifierNameSyntax comp in name.TypeArgumentList.Arguments)
			{
				var model = new Model<ReturnType>();
				model.Set("compName".AsSpan(), Parameter.Create(comp.Identifier.Text));
				model.Set("varName".AsSpan(), Parameter.Create(comp.Identifier.Text));

				models.Add(model);
			}

			return models.ToArray();
		}
	}
}
