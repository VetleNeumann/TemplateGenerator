//HintName: Ecs_ArchType.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Test
{
	public partial struct Ecs
	{
		public partial struct ArchType1 : IArchType<ArchType1, Position, Position.Vectorized, Position.Array>, IArchType<ArchType1, Velocity, Velocity.Vectorized, Velocity.Array>
		{
			public Position.Vectorized Position;
			public Velocity.Vectorized Velocity;
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Position.Array IArchType<ArchType1, Position, Position.Vectorized, Position.Array>.GetSingle(ref ArchType1 arch)
			{
				return ref Unsafe.As<Position.Vectorized, Position.Array>(ref arch.Position);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Position.Vectorized IArchType<ArchType1, Position, Position.Vectorized, Position.Array>.GetVec(ref ArchType1 arch)
			{
				return ref arch.Position;
			}
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Velocity.Array IArchType<ArchType1, Velocity, Velocity.Vectorized, Velocity.Array>.GetSingle(ref ArchType1 arch)
			{
				return ref Unsafe.As<Velocity.Vectorized, Velocity.Array>(ref arch.Velocity);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Velocity.Vectorized IArchType<ArchType1, Velocity, Velocity.Vectorized, Velocity.Array>.GetVec(ref ArchType1 arch)
			{
				return ref arch.Velocity;
			}
		}

		public partial struct ArchType2 : IArchType<ArchType2, Position, Position.Vectorized, Position.Array>
		{
			public Position.Vectorized Position;
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Position.Array IArchType<ArchType2, Position, Position.Vectorized, Position.Array>.GetSingle(ref ArchType2 arch)
			{
				return ref Unsafe.As<Position.Vectorized, Position.Array>(ref arch.Position);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ref Position.Vectorized IArchType<ArchType2, Position, Position.Vectorized, Position.Array>.GetVec(ref ArchType2 arch)
			{
				return ref arch.Position;
			}
		}
	}
}