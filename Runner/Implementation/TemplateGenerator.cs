﻿using Microsoft.CodeAnalysis;
using TemplateGenerator;

namespace EnCS.Generator
{
	[Generator]
	public class TemplateGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var compGenerator = new ComponentGenerator();

			new TemplateGeneratorBuilder()
				.WithLogging(context)
				.WithAttributeGenerator("ComponentAttribute", context, compGenerator)
				.WithInfoFile(context);

			/*
			TemplateGeneratorHelpers.RegisterAttributeTemplateGenerator("ComponentAttribute", context, compGenerator);
			*/
		}
	}
}
