//HintName: Ecs_ArchType.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Test
{
	public partial class Ecs
	{
		public struct Wall : IArchType<Wall, Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>, IArchType<Wall, Project.Primitives.Velocity, Project.Primitives.Velocity.Vectorized, Project.Primitives.Velocity.Array>
		{
			public Project.Primitives.Position.Vectorized Position;
			public Project.Primitives.Velocity.Vectorized Velocity;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Project.Primitives.Position.Array IArchType<Wall, Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>.GetSingle(ref Wall arch)
			{
				return ref Unsafe.As<Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>(ref arch.Position);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Project.Primitives.Position.Vectorized IArchType<Wall, Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>.GetVec(ref Wall arch)
			{
				return ref arch.Position;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Project.Primitives.Velocity.Array IArchType<Wall, Project.Primitives.Velocity, Project.Primitives.Velocity.Vectorized, Project.Primitives.Velocity.Array>.GetSingle(ref Wall arch)
			{
				return ref Unsafe.As<Project.Primitives.Velocity.Vectorized, Project.Primitives.Velocity.Array>(ref arch.Velocity);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Project.Primitives.Velocity.Vectorized IArchType<Wall, Project.Primitives.Velocity, Project.Primitives.Velocity.Vectorized, Project.Primitives.Velocity.Array>.GetVec(ref Wall arch)
			{
				return ref arch.Velocity;
			}

			public ref struct Ref
			{
				public Project.Primitives.Position.Ref Position;
				public Project.Primitives.Velocity.Ref Velocity;

				public Ref(Project.Primitives.Position.Ref Position, Project.Primitives.Velocity.Ref Velocity)
				{
					this.Position = Position;
					this.Velocity = Velocity;
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public static Ref FromArchType(ref Wall archType, int idx)
				{
					return new Ref(
						Project.Primitives.Position.Ref.FromArray(ref Unsafe.As<Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>(ref archType.Position), idx), 
						Project.Primitives.Velocity.Ref.FromArray(ref Unsafe.As<Project.Primitives.Velocity.Vectorized, Project.Primitives.Velocity.Array>(ref archType.Velocity), idx)
					);
				}
			}
		}

		public struct Tile : IArchType<Tile, Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>
		{
			public Project.Primitives.Position.Vectorized Position;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Project.Primitives.Position.Array IArchType<Tile, Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>.GetSingle(ref Tile arch)
			{
				return ref Unsafe.As<Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>(ref arch.Position);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Project.Primitives.Position.Vectorized IArchType<Tile, Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>.GetVec(ref Tile arch)
			{
				return ref arch.Position;
			}

			public ref struct Ref
			{
				public Project.Primitives.Position.Ref Position;

				public Ref(Project.Primitives.Position.Ref Position)
				{
					this.Position = Position;
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public static Ref FromArchType(ref Tile archType, int idx)
				{
					return new Ref(
						Project.Primitives.Position.Ref.FromArray(ref Unsafe.As<Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>(ref archType.Position), idx)
					);
				}
			}
		}
	}

	public static class Ecs_ContainerExtensions
	{
		// TODO: Generate create method

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Ecs.Wall.Ref Get(this ref ArchTypeContainer<Ecs.Wall> container, ArchRef<Ecs.Wall> ptr)
		{
			return Ecs.Wall.Ref.FromArchType(ref container.GetVec(ptr), (int)ptr.idx & 7);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Ecs.Tile.Ref Get(this ref ArchTypeContainer<Ecs.Tile> container, ArchRef<Ecs.Tile> ptr)
		{
			return Ecs.Tile.Ref.FromArchType(ref container.GetVec(ptr), (int)ptr.idx & 7);
		}
	}
}