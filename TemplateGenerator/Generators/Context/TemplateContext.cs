using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TemplateGenerator
{
	class TemplateContext<TContext> : ITemplateContext<TContext>
	{
		public TContext Context { get; }

		Dictionary<Guid, Action> cache = new();
		List<Guid> invalidated = new();

        public TemplateContext(TContext context)
        {
			this.Context = context;
        }

        public void Execute<TNode>(ImmutableArray<TNode> nodeArray, SourceProductionContext generatorContext, ITemplateSourceGenerator<TNode, TContext> generator) where TNode : SyntaxNode
		{
			var action = () => TemplateGeneratorHelpers.ExecuteGenerator(nodeArray, generatorContext, generator, this);

			if (!cache.ContainsKey(generator.Id))
			{
				cache[generator.Id] = action;
			}
			else
			{
				cache.Add(generator.Id, action);
			}

			cache[generator.Id]();

			ExecuteInvalidated();
		}

		public void ExecuteInvalidated()
		{
			foreach (Guid id in invalidated)
			{
				if (cache.ContainsKey(id))
					cache[id]();
			}

			cache.Clear();
		}

		public void Invalidate(Guid id)
		{
			invalidated.Add(id);
		}
	}
}
