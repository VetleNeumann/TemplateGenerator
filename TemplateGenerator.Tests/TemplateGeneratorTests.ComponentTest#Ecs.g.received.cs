//HintName: Ecs.g.cs
using System.Runtime.CompilerServices;
using EnCS;

namespace Runner
{
	public partial struct Ecs
	{
		ArchTypeContainer<ArchType1> container1;
		ArchTypeContainer<ArchType2> container2;

		public Ecs()
		{
			container1 = new ArchTypeContainer<ArchType1>(10);
			container2 = new ArchTypeContainer<ArchType2>(5);
		}

		public void Loop(PositionSystem system)
		{
			var enum1 = new ComponentEnumerableNew<Position, Position.Vectorized, Position.Array>.Enumerator<ArchType1>(container1.AsSpan());
			var enum2 = new ComponentEnumerableNew<Position, Position.Vectorized, Position.Array>.Enumerator<ArchType2>(container2.AsSpan());
			
			system.Update(ref enum1);
			system.Update(ref enum2);
		}
	}

	static class Ecs_Intercept
	{
		[InterceptsLocation(@"", 144, 5)]
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public static Ecs InterceptBuild(this EcsBuilder builder)
		{
			return new Ecs();
		}
	}
}