using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Runner
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string source = @"
using namespace Project.Primitives;

public struct Mesh
{
	public string name;
}

[ResourceManager]
public partial class MeshResourceManager : IResourceManager<Mesh>
{

}

[Component]
public partial struct InvalidComp
{
	public string x;
}

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

[ComponentAttribute]
public partial struct Scale
{
	public float x;
	public float y;
	public float z;

	public static implicit operator Scale(Vector3 v) => new Scale(v.X, v.Y, v.Z);
	public static implicit operator Scale(Vector2 v) => new Scale(v.X, v.Y, 0);
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

	public void UpdateAfter(Position.Ref position)
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
				x.ArchType<InvalidComp>(""IsWrong"");
				x.ArchType<Position, Velocity, Mesh>(""Wall"");
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
				x.World<Ecs.Wall>();
			})
			.Resource(x =>
			{
				x.ResourceManager<MeshResourceManager>();
			})
			.Build<Ecs>();

		var ecs = new EcsBuilder()
			.ArchType(x =>
			{
				x.ArchType<Position>(""Tile"");
			})
			.System(x =>
			{
				x.System<PositionSystem>();
			})
			.World(x =>
			{
				x.World<Ecs.Tile>(""Main"");
			})
			.Resource(x =>
			{
			})
			.Build<Ecs2>();
	}
}
";

			SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
			CSharpCompilation compilation = CSharpCompilation.Create("Tests", new[] { syntaxTree });

			GeneratorDriver driver = CSharpGeneratorDriver.Create(new EnCS.Generator.TemplateGenerator());
			driver = driver.RunGenerators(compilation);
		}
	}
}
