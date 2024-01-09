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

public struct TestContext
{
	public float data;
}

public struct Mesh
{
	public string name;
}

public struct MeshId
{
	public uint id;
}

public struct Kaki
{
	public string name;
}

public struct KakiId
{
	public uint id;
}

[ResourceManager]
public partial class TestResourceManager : IResourceManager<Kaki, KakiId>
{

}

[ResourceManager]
public partial class MeshResourceManager : IResourceManager<Mesh, MeshId>
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

[SystemAttribute<TestContext>]
[UsingResource<MeshResourceManager>]
[UsingResource<TestResourceManager>]
public partial class ResourceSystem
{
	[SystemPreLoop, SystemLayer(0)]
	public void PreLoop1()
	{

	}

	[SystemUpdate, SystemLayer(1)]
	public void Update(ref Scale.Vectorized scale)
	{
    }

	[SystemUpdate, SystemLayer(1)]
	public void Update(Scale.Ref scale)
	{
    }

	[SystemPostLoop, SystemLayer(0)]
	public void PostLoop1()
	{

	}

	[SystemPreLoop, SystemLayer(1)]
	public void PreLoop()
	{

	}

	[SystemUpdate, SystemLayer(0, 16)]
	public void Update(ref TestContext context, Position.Ref position, Velocity.Ref velocity, ref MeshId mesh, ref KakiId kaki)
	{
    }

	[SystemUpdate, SystemLayer(0, 16)]
	public void Update(ref TestContext context, ref Position.Vectorized position, ref Velocity.Vectorized velocity)
	{
	}

	[SystemPostLoop, SystemLayer(1)]
	public void PostLoop()
	{

	}
}

[SystemAttribute]
public partial class TestSystem
{
	[SystemUpdate]
	public void Update(Scale.Ref scale)
	{
	}

	[SystemUpdate]
	public void Update(Position.Ref position, Velocity.Ref velocity)
	{
	}
}

[SystemAttribute]
public partial class PositionSystem
{
	[SystemUpdate]
	public void Update(Position.Ref position)
	{
    }

	[SystemUpdate]
	public void Update(ref Position.Vectorized position)
	{
	}

	[SystemUpdate]
	public void UpdateAfter(Position.Ref position)
	{
    }
}

[SystemAttribute]
public partial class VelocitySystem
{
	[SystemUpdate]
	public void Update(Position.Ref position, Velocity.Ref velocity)
	{
    }

	[SystemUpdate]
	public void Update(ref Position.Vectorized position,ref Velocity.Vectorized velocity)
	{
	}
}

[ComposedSystem<VelocitySystem>(layer = 0)]
[ComposedSystem<ResourceSystem>(layer = 1, chunk = 8)]
public partial class ComposedSystem
{

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
				x.ArchType<Position, Velocity, Mesh, Kaki>(""Wall"");
				x.ArchType<Position, Scale>(""Tile"");
			})
			.System(x =>
			{
				x.System<PositionSystem>();
				x.System<VelocitySystem>();
				x.System<ResourceSystem>();
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
				x.ResourceManager<TestResourceManager>();
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
			for (int i = 0; i < 100; i++)
			{
                Console.WriteLine(i);
                driver.RunGenerators(compilation);
			}
            Console.WriteLine("Done!");
        }
	}
}
