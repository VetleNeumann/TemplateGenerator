using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TemplateGenerator
{
	class WorldGenerator : ITemplateSourceGenerator<IdentifierNameSyntax>
	{
		public Guid Id { get; } = Guid.NewGuid();

		public string Template => "World.tcs";

        public Model<ReturnType> CreateModel(Compilation compilation, IdentifierNameSyntax node)
		{
			var builderRoot = EcsGenerator.GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var worldStep = builderSteps.First(x => x.Name.Identifier.Text == "World");
			var systemStep = builderSteps.First(x => x.Name.Identifier.Text == "System");
			var archTypeStep = builderSteps.First(x => x.Name.Identifier.Text == "ArchType");

			var systems = GetSystemNames(systemStep);
			var archTypeComponents = GetArchTypeComponents(archTypeStep);

			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("ecsName".AsSpan(), new Parameter<string>("Ecs"));
			model.Set("worlds".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetWorlds(compilation, worldStep, systems, archTypeComponents)));

			return model;
		}

		public bool Filter(GeneratorSyntaxContext context, IdentifierNameSyntax node)
		{
			return node.Identifier.Text == "EcsBuilder";
		}

		public string GetName(IdentifierNameSyntax node)
		{
			return $"{EcsGenerator.GetEcsName(node)}_World";
		}

		static List<Model<ReturnType>> GetWorlds(Compilation compilation, MemberAccessExpressionSyntax step, List<string> systems, Dictionary<string, List<string>> archTypeComponents)
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

				if (genericName.Identifier.Text != "World")
					continue;

				var nameArg = invocation.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
				var nameToken = nameArg.Token.ValueText;

				var archTypes = GetArchTypes(genericName);

				var model = new Model<ReturnType>();
				model.Set("worldName".AsSpan(), Parameter.Create(nameToken));
				model.Set("archTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(archTypes.ToParameter().ToModel("archTypeName").ToList()));
				model.Set("systems".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetSystems(compilation, archTypes, systems, archTypeComponents)));

				models.Add(model);

				i++;
			}

			return models;
		}

		static List<string> GetArchTypes(GenericNameSyntax name)
		{
			var archTypes = new List<string>();

			foreach (IdentifierNameSyntax comp in name.TypeArgumentList.Arguments)
			{
				archTypes.Add(comp.Identifier.Text);
			}

			return archTypes;
		}

		static Model<ReturnType>[] GetSystems(Compilation compilation, List<string> worldArchTypes, List<string> systems, Dictionary<string, List<string>> archTypeComponents)
		{
			var models = new List<Model<ReturnType>>();

			var worldComponents = worldArchTypes.SelectMany(x => archTypeComponents[x]).ToList();

			foreach (string system in systems)
			{
				var systemComps = GetSystemComponents(compilation, system);

				if (!systemComps.All(worldComponents.Contains))
					continue;

				var model = new Model<ReturnType>();
				model.Set("containers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetComptaibleContainers(systemComps, worldArchTypes, archTypeComponents)));
				model.Set("systemName".AsSpan(), Parameter.Create(system));

				models.Add(model);
			}

			return models.ToArray();
		}

		static List<Model<ReturnType>> GetComptaibleContainers(List<string> systemComps, List<string> worldArchTypes, Dictionary<string, List<string>> archTypeComponents)
		{
			List<Model<ReturnType>> models = new();

			foreach (var archType in worldArchTypes)
			{
				if (!systemComps.All(archTypeComponents[archType].Contains))
					continue;

				var compModels = systemComps.ToParameter().ToModel("compName").ToList();

				var model = new Model<ReturnType>();
				model.Set("archTypeName".AsSpan(), Parameter.Create(archType));
				model.Set("components".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(compModels));

				models.Add(model);
			}

			return models;
		}

		static List<string> GetSystemComponents(Compilation compilation, string name)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			var systemNode = FindNode<ClassDeclarationSyntax>(nodes, x => x.Identifier.Text == name);

			List<string> names = new List<string>();
			foreach (var method in systemNode.Members.Where(x => x is MethodDeclarationSyntax).Select(x => x as MethodDeclarationSyntax))
			{
				if (method.Identifier.Text != "Update")
					continue;

				foreach (var parameter in method.ParameterList.Parameters)
				{
					var paramType = parameter.Type as QualifiedNameSyntax;

					names.Add((paramType.Left as IdentifierNameSyntax).Identifier.Text);
				}

				// TODO: Only do first method for now
				break;
			}

			return names;
		}

		static T FindNode<T>(IEnumerable<SyntaxNode> nodes, Func<T, bool> predicate) where T : SyntaxNode
		{
			return nodes.Where(x => x is T).Cast<T>().Single(predicate);
		}

		static List<string> GetSystemNames(MemberAccessExpressionSyntax step)
		{
			var systems = new List<string>();

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
					systems.Add(comp.Identifier.Text);
				}
			}

			return systems;
		}

		static Dictionary<string, List<string>> GetArchTypeComponents(MemberAccessExpressionSyntax step)
		{
			Dictionary<string, List<string>> archTypeComponents = new();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			foreach (var archType in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (archType.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
					continue;

				if (memberAccess.Name is not GenericNameSyntax genericName)
					continue;

				if (genericName.Identifier.Text != "ArchType")
					continue;

				var nameArg = invocation.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;

				var components = new List<string>();
				foreach (IdentifierNameSyntax comp in genericName.TypeArgumentList.Arguments)
				{
					components.Add(comp.Identifier.Text);
				}

				archTypeComponents.Add(nameArg.Token.ValueText, components);

			}

			return archTypeComponents;
		}
	}
}
