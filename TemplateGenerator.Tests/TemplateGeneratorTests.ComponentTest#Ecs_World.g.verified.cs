//HintName: Ecs_World.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Test
{
	public partial class Ecs
	{
		public class Main
		{
			ArchTypeContainer<Wall> containerWall;
			ArchTypeContainer<Tile> containerTile;

			public Main()
			{
				containerWall = new ArchTypeContainer<Wall>();
				containerTile = new ArchTypeContainer<Tile>();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ArchRef<Wall> Create(in Wall data)
			{
				return containerWall.Create(data);
			}
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Wall.Ref Get(in ArchRef<Wall> ptr)
			{
				return containerWall.Get(ptr);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ArchRef<Tile> Create(in Tile data)
			{
				return containerTile.Create(data);
			}
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Tile.Ref Get(in ArchRef<Tile> ptr)
			{
				return containerTile.Get(ptr);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Loop(PositionSystem system)
			{
				var enumWall = new ComponentEnumerableNew<Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>.Enumerator<Wall>(containerWall.AsSpan());
				var enumTile = new ComponentEnumerableNew<Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>.Enumerator<Tile>(containerTile.AsSpan());
				
				system.Update(ref enumWall);
				system.Update(ref enumTile);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Loop(VelocitySystem system)
			{
				var enumWall = new ComponentEnumerableNew<Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array, Project.Primitives.Velocity, Project.Primitives.Velocity.Vectorized, Project.Primitives.Velocity.Array>.Enumerator<Wall>(containerWall.AsSpan());
				
				system.Update(ref enumWall);
			}
		}

		public class World2
		{
			ArchTypeContainer<Wall> containerWall;

			public World2()
			{
				containerWall = new ArchTypeContainer<Wall>();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ArchRef<Wall> Create(in Wall data)
			{
				return containerWall.Create(data);
			}
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Wall.Ref Get(in ArchRef<Wall> ptr)
			{
				return containerWall.Get(ptr);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Loop(PositionSystem system)
			{
				var enumWall = new ComponentEnumerableNew<Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>.Enumerator<Wall>(containerWall.AsSpan());
				
				system.Update(ref enumWall);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Loop(VelocitySystem system)
			{
				var enumWall = new ComponentEnumerableNew<Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array, Project.Primitives.Velocity, Project.Primitives.Velocity.Vectorized, Project.Primitives.Velocity.Array>.Enumerator<Wall>(containerWall.AsSpan());
				
				system.Update(ref enumWall);
			}
		}
	}
}