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
			var archTypeStep = builderSteps.First(x => x.Name.Identifier.Text == "ArchType");
			var systemStep = builderSteps.First(x => x.Name.Identifier.Text == "System");

			var builderLocation = buildStep.Name.GetLocation();
			var loc = builderLocation.GetMappedLineSpan();

			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("name".AsSpan(), Parameter.Create(GetEcsName(node)));
			model.Set("path".AsSpan(), Parameter.Create(loc.Path));
			model.Set("line".AsSpan(), Parameter.Create<float>(loc.StartLinePosition.Line + 1));
			model.Set("character".AsSpan(), Parameter.Create<float>(loc.StartLinePosition.Character + 1));

			Dictionary<string, List<string>> archTypeComponents = new();

			model.Set("archTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetArchTypes(archTypeStep, archTypeComponents)));
			model.Set("systems".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetSystemsNew(compilation, systemStep, archTypeComponents)));

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

		static List<Model<ReturnType>> GetArchTypes(MemberAccessExpressionSyntax step, Dictionary<string, List<string>> archTypeComponents)
		{
			List<Model<ReturnType>> models = new();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			int i = 0;
			foreach (var archType in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (archType.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
					continue;

				if (memberAccess.Name is not GenericNameSyntax genericName)
					continue;

				var model = new Model<ReturnType>();

				i++;
				model.Set("name".AsSpan(), Parameter.Create($"ArchType{i}"));
				model.Set("i".AsSpan(), Parameter.Create<float>(i));
				model.Set("size".AsSpan(), Parameter.Create(invocation.ArgumentList.Arguments[0].ToString()));

				archTypeComponents.Add($"ArchType{i}", GetArchTypeComponents(genericName));

				models.Add(model);
			}

			return models;
		}

		static List<string> GetArchTypeComponents(GenericNameSyntax name)
		{
			var components = new List<string>();

			foreach (IdentifierNameSyntax comp in name.TypeArgumentList.Arguments)
			{
				components.Add(comp.Identifier.Text);
			}

			return components;
		}

		static List<Model<ReturnType>> GetSystemsNew(Compilation compilation, MemberAccessExpressionSyntax step, Dictionary<string, List<string>> archTypeComponents)
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

				foreach (string systemName in genericName.TypeArgumentList.Arguments.Select(x => x.ToString()))
				{
					var model = new Model<ReturnType>();
					model.Set("containers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(FindCompatibleContainers(compilation, systemName, archTypeComponents)));
					model.Set("name".AsSpan(), Parameter.Create(systemName));

					models.Add(model);
				}
			}

			return models;
		}

		static List<Model<ReturnType>> FindCompatibleContainers(Compilation compilation, string system, Dictionary<string, List<string>> archTypeComponents)
		{
			List<Model<ReturnType>> models = new();

			int idx = 0;
			foreach (var kvp in archTypeComponents)
			{
				var comps = GetSystemComponentNames(compilation, system);

				if (!comps.All(kvp.Value.Contains))
					continue;

				idx++;

				var model = new Model<ReturnType>();
				model.Set("archType".AsSpan(), Parameter.Create(kvp.Key));
				model.Set("i".AsSpan(), Parameter.Create<float>(idx));
				model.Set("name".AsSpan(), Parameter.Create(system));
				model.Set("components".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetSystemComponents(compilation, system)));

				models.Add(model);
			}

			return models;
		}

		static List<Model<ReturnType>> GetSystemComponents(Compilation compilation, string name)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
			var systemNode = FindNode<ClassDeclarationSyntax>(nodes, x => x.Identifier.Text == name);

			List<Model<ReturnType>> models = new();
			foreach (var method in systemNode.Members.Where(x => x is MethodDeclarationSyntax).Select(x => x as MethodDeclarationSyntax))
			{
				if (method.Identifier.Text != "Update")
					continue;

				foreach (var parameter in method.ParameterList.Parameters)
				{
					var paramType = parameter.Type as QualifiedNameSyntax;

					var model = new Model<ReturnType>();
					model.Set("name".AsSpan(), Parameter.Create((paramType.Left as IdentifierNameSyntax).Identifier.Text));

					models.Add(model);
				}

				// TODO: Only do first method for now
				break;
			}

			return models;
		}

		static List<string> GetSystemComponentNames(Compilation compilation, string name)
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
	}
}
