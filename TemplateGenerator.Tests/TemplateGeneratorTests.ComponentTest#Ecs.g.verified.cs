//HintName: Ecs.g.cs
using System.Runtime.CompilerServices;
using EnCS;

namespace Test
{
	public partial class Ecs
	{
		Main worldMain;
		World2 worldWorld2;

		public Ecs()
		{
			worldMain = new Main();
			worldWorld2 = new World2();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref Main GetMain()
		{
			return ref worldMain;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref World2 GetWorld2()
		{
			return ref worldWorld2;
		}
	}

	static class Ecs_Intercept
	{
		[InterceptsLocation(@"", 73, 5)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public static Ecs InterceptBuild(this EcsBuilder builder)
		{
			return new Ecs();
		}
	}
}