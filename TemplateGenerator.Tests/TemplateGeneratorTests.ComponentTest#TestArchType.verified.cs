//HintName: TestArchType.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Project
{
	public partial struct TestArchType : IArchType<TestArchType, Position, Position.Vectorized, Position.Array>, IArchType<TestArchType, Velocity, Velocity.Vectorized, Velocity.Array>
	{
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ref Position.Array IArchType<TestArchType, Position, Position.Vectorized, Position.Array>.GetSingle(ref TestArchType arch)
		{
			return ref Unsafe.As<Position.Vectorized, Position.Array>(ref arch.position);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ref Position.Vectorized IArchType<TestArchType, Position, Position.Vectorized, Position.Array>.GetVec(ref TestArchType arch)
		{
			return ref arch.position;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ref Velocity.Array IArchType<TestArchType, Velocity, Velocity.Vectorized, Velocity.Array>.GetSingle(ref TestArchType arch)
		{
			return ref Unsafe.As<Velocity.Vectorized, Velocity.Array>(ref arch.position);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ref Velocity.Vectorized IArchType<TestArchType, Velocity, Velocity.Vectorized, Velocity.Array>.GetVec(ref TestArchType arch)
		{
			return ref arch.position;
		}
	}
}