//HintName: Ecs_ArchType.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Test
{
	public partial struct Ecs
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
		}
	}
}