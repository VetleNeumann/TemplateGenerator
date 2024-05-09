using Microsoft.CodeAnalysis;
using TemplateGenerator;

namespace EnCS.Generator
{
	[Generator]
	public class TemplateGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var compGenerator = new ComponentGenerator();

			TemplateGeneratorHelpers.RegisterAttributeTemplateGenerator("ComponentAttribute", context, compGenerator);
		}
	}
}
