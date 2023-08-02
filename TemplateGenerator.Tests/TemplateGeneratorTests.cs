using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TemplateGenerator.Tests
{
	public static class TestHelper
	{
		public static Task Verify(string source)
		{
			SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
			CSharpCompilation compilation = CSharpCompilation.Create("Tests", new[] { syntaxTree });

			GeneratorDriver driver = CSharpGeneratorDriver.Create(new TemplateGenerator());
			driver = driver.RunGenerators(compilation);

			return Verifier.Verify(driver);
		}
	}

	[UsesVerify]
	public class TemplateGeneratorTests
	{
		[Fact]
		public Task ComponentTest()
		{
			string source = @"
using namespace Project;

[ComponentAttribute]
public partial struct Position
{
	public int x;
	public int y;
	public int z;
}

[ComponentAttribute]
public partial struct Velocity
{
	public float x;
	public int y;
	public int z;
}

[ArchTypeAttribute]
public partial struct TestArchType
{
	public Position.Vectorized position;
	public Velocity.Vectorized velocity;
}

[SystemAttribute]
public partial class PositionSystem
{
	public void Update(Position.Ref position)
	{
    }

	public void Update(ref Position.Vectorized position)
	{
	}
}
";
			return TestHelper.Verify(source);
		}
	}
}