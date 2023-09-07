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
	public FixedArray4<int> z;
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

[EcsAttribute<PositionSystem>]
public partial struct Ecs
{

}
";
			string source2 = @"
using namespace Project;

[ComponentAttribute]
public partial struct Position
{
	public int x;
	public int y;
	public FixedArray4<int> z;
}

[ComponentAttribute]
public partial struct Velocity
{
	public float x;
	public double y;
	public decimal z;
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

[SystemAttribute]
public partial class VelocitySystem
{
	public void Update(Position.Ref position, Velocity.Ref velocity)
	{
    }

	public void Update(ref Position.Vectorized position,ref Velocity.Vectorized velocity)
	{
	}
}

public partial struct Ecs
{

}

namespace Test
{
	static void Main()
	{
		new EcsBuilder()
			.ArchType(x =>
			{
				x.ArchType<Position, Velocity>(""Wall"");
				x.ArchType<Position>(""Tile"");
			})
			.System(x =>
			{
				x.System<PositionSystem>();
				x.System<VelocitySystem>();
			})
			.World(x =>
			{
				x.World<Ecs.Wall, Ecs.Tile>(""Main"");
				x.World<Ecs.Wall>(""World2"");
			})
			.Build<Ecs>();
	}
}
";

			var source3 = File.ReadAllText("Files/TestFile.txt");

			return TestHelper.Verify(source2);
		}
	}
}