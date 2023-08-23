using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TemplateGenerator
{
	class EcsGenerator : ITemplateSourceGenerator<ConstructorDeclarationSyntax>
	{
		public Guid Id { get; } = Guid.NewGuid();

		public string Template => "Ecs.tcs";

		public Model<ReturnType> CreateModel(Compilation compilation, ConstructorDeclarationSyntax node)
		{
			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("name".AsSpan(), Parameter.Create(GetEcsName(node)));
			model.Set("archTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetMembers(compilation, node)));

			return model;
		}

		public bool Filter(GeneratorSyntaxContext context, ConstructorDeclarationSyntax node)
		{
			return node.Identifier.Text == "EcsBuilder";
		}

		public string GetName(ConstructorDeclarationSyntax node)
		{
			return GetEcsName(node);
		}

		static string GetEcsName(ConstructorDeclarationSyntax node)
		{
			var buildStep = node.Parent.DescendantNodes()
				.Where(x => x is IncompleteMemberSyntax)
				.Cast<IncompleteMemberSyntax>()
				.Single(x => (x.Type as GenericNameSyntax)?.Identifier.Text == "Build");

			var type = buildStep.Type as GenericNameSyntax;

			var ecsType = type.TypeArgumentList.Arguments[0] as SimpleNameSyntax;

			return ecsType.Identifier.Text;
		}

		static Model<ReturnType>[] GetMembers(Compilation compilation, ConstructorDeclarationSyntax node)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			var builderSteps = node.Parent.DescendantNodes().Where(x => x is ConstructorDeclarationSyntax).Cast<ConstructorDeclarationSyntax>();

			var models = new List<Model<ReturnType>>();

			foreach (var step in builderSteps)
			{
				switch (step.Identifier.Text)
				{
					case "Config":
						break;
					case "ArchType":
						models.AddRange(GetArchType(step));
						break;
					case "System":
						break;
					default:
						break;
				}

				if (step.Identifier.Text == "Build")
					break;

				//models.Add(model);
			}

			return models.ToArray();
		}

		static IEnumerable<Model<ReturnType>> GetArchType(ConstructorDeclarationSyntax builder)
		{
			int i = 0;
			foreach (var statement in builder.Body.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (statement.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
					continue;

				var model = new Model<ReturnType>();

				i++;
				model.Set("name".AsSpan(), Parameter.Create($"ArchType{i}"));
				model.Set("i".AsSpan(), Parameter.Create<float>(i));

				yield return model;
            }
		}

		static T FindNode<T>(IEnumerable<SyntaxNode> nodes, Func<T, bool> predicate) where T : SyntaxNode
		{
			return nodes.Where(x => x is T).Cast<T>().Single(predicate);
		}
	}
}
