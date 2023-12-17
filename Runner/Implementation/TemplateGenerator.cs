using Microsoft.CodeAnalysis;
using TemplateGenerator;

namespace EnCS.Generator
{
	[Generator]
	public class TemplateGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var ecsGenerator = new EcsGenerator();
			var compGenerator = new ComponentGenerator();
			var archTypeGenerator = new ArchTypeGenerator();
			var systemGenerator = new SystemGenerator();
			var worldGenerator = new WorldGenerator();
			var resourceManagerGenerator = new ResourceManagerGenerator();

			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, compGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, archTypeGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, systemGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, worldGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, ecsGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, resourceManagerGenerator);
		}
	}
}
