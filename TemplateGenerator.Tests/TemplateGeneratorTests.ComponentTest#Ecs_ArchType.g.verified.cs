//HintName: Ecs_ArchType.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Test
{
	public partial class Ecs
	{
		public struct Wall : IArchType<Wall, Position, Position.Vectorized, Position.Array>, IArchType<Wall, Velocity, Velocity.Vectorized, Velocity.Array>
		{
			public Position.Vectorized Position;
			public Velocity.Vectorized Velocity;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Position.Array IArchType<Wall, Position, Position.Vectorized, Position.Array>.GetSingle(ref Wall arch)
			{
				return ref Unsafe.As<Position.Vectorized, Position.Array>(ref arch.Position);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Position.Vectorized IArchType<Wall, Position, Position.Vectorized, Position.Array>.GetVec(ref Wall arch)
			{
				return ref arch.Position;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Velocity.Array IArchType<Wall, Velocity, Velocity.Vectorized, Velocity.Array>.GetSingle(ref Wall arch)
			{
				return ref Unsafe.As<Velocity.Vectorized, Velocity.Array>(ref arch.Velocity);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Velocity.Vectorized IArchType<Wall, Velocity, Velocity.Vectorized, Velocity.Array>.GetVec(ref Wall arch)
			{
				return ref arch.Velocity;
			}

			public ref struct Ref
			{
				public Position.Ref Position;
				public Velocity.Ref Velocity;

				public Ref(Position.Ref Position, Velocity.Ref Velocity)
				{
					this.Position = Position;
					this.Velocity = Velocity;
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public static Ref FromArchType(ref Wall archType, int idx)
				{
					return new Ref(
						Test.Position.Ref.FromArray(ref Unsafe.As<Position.Vectorized, Position.Array>(ref archType.Position), idx), 
						Test.Velocity.Ref.FromArray(ref Unsafe.As<Velocity.Vectorized, Velocity.Array>(ref archType.Velocity), idx)
					);
				}
			}
		}

		public struct Tile : IArchType<Tile, Position, Position.Vectorized, Position.Array>
		{
			public Position.Vectorized Position;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Position.Array IArchType<Tile, Position, Position.Vectorized, Position.Array>.GetSingle(ref Tile arch)
			{
				return ref Unsafe.As<Position.Vectorized, Position.Array>(ref arch.Position);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Position.Vectorized IArchType<Tile, Position, Position.Vectorized, Position.Array>.GetVec(ref Tile arch)
			{
				return ref arch.Position;
			}

			public ref struct Ref
			{
				public Position.Ref Position;

				public Ref(Position.Ref Position)
				{
					this.Position = Position;
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public static Ref FromArchType(ref Tile archType, int idx)
				{
					return new Ref(
						Test.Position.Ref.FromArray(ref Unsafe.As<Position.Vectorized, Position.Array>(ref archType.Position), idx)
					);
				}
			}
		}
	}

	public static class Ecs_ContainerExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Ecs.Wall.Ref Get(this ref ArchTypeContainer<Ecs.Wall> container, ArchRef<Ecs.Wall> ptr)
		{
			return Ecs.Wall.Ref.FromArchType(ref container.GetVec(ptr), (int)ptr.idx & 7); // TODO: Fix anding
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Ecs.Tile.Ref Get(this ref ArchTypeContainer<Ecs.Tile> container, ArchRef<Ecs.Tile> ptr)
		{
			return Ecs.Tile.Ref.FromArchType(ref container.GetVec(ptr), (int)ptr.idx & 7); // TODO: Fix anding
		}
	}
}