//HintName: Ecs_World.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Test
{
	public partial struct Ecs
	{
		public struct Main
		{
			ArchTypeContainer<Wall> containerWall;
			ArchTypeContainer<Tile> containerTile;

			public Main()
			{
				containerWall = new ArchTypeContainer<Wall>();
				containerTile = new ArchTypeContainer<Tile>();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Ref<Wall> Create(in Wall data)
			{
				return containerWall.Create(data);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Ref<Tile> Create(in Tile data)
			{
				return containerTile.Create(data);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Loop(PositionSystem system)
			{
				var enumWall = new ComponentEnumerableNew<Position, Position.Vectorized, Position.Array>.Enumerator<Wall>(containerWall.AsSpan());
				var enumTile = new ComponentEnumerableNew<Position, Position.Vectorized, Position.Array>.Enumerator<Tile>(containerTile.AsSpan());
				
				system.Update(ref enumWall);
				system.Update(ref enumTile);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Loop(VelocitySystem system)
			{
				var enumWall = new ComponentEnumerableNew<Position, Position.Vectorized, Position.Array, Velocity, Velocity.Vectorized, Velocity.Array>.Enumerator<Wall>(containerWall.AsSpan());
				
				system.Update(ref enumWall);
			}
		}

		public struct World2
		{
			ArchTypeContainer<Wall> containerWall;

			public World2()
			{
				containerWall = new ArchTypeContainer<Wall>();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Ref<Wall> Create(in Wall data)
			{
				return containerWall.Create(data);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Loop(PositionSystem system)
			{
				var enumWall = new ComponentEnumerableNew<Position, Position.Vectorized, Position.Array>.Enumerator<Wall>(containerWall.AsSpan());
				
				system.Update(ref enumWall);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Loop(VelocitySystem system)
			{
				var enumWall = new ComponentEnumerableNew<Position, Position.Vectorized, Position.Array, Velocity, Velocity.Vectorized, Velocity.Array>.Enumerator<Wall>(containerWall.AsSpan());
				
				system.Update(ref enumWall);
			}
		}
	}
}