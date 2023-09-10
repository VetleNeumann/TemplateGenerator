//HintName: Velocity.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Project
{
	public partial struct Velocity : IComponent<Velocity, Velocity.Vectorized, Velocity.Array>
	{
		public struct Vectorized
		{
			public Vector256<float> x;
			public Vector512<double> y;
			public FixedArray2<Vector512<decimal>> z;
		}

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public struct Array
		{
			public const int Size = 8;

			public FixedArray8<float> x;
			public FixedArray8<double> y;
			public FixedArray8<decimal> z;
		}

		public ref struct Ref
		{
			public ref float x;
			public ref double y;
			public ref decimal z;
			
			public Ref(ref float x, ref double y, ref decimal z)
			{
				this.x = ref x;
				this.y = ref y;
				this.z = ref z;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static Ref FromArray(ref Array array, int idx)
			{
				return new Ref(ref array.x[idx], ref array.y[idx], ref array.z[idx]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref Vectorized GetVec<TArch>(ref TArch arch) where TArch : unmanaged, IArchType<TArch, Velocity, Vectorized, Array>
		{
			return ref TArch.GetVec(ref arch);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref Array GetSingle<TArch>(ref TArch arch) where TArch : unmanaged, IArchType<TArch, Velocity, Vectorized, Array>
		{
			return ref TArch.GetSingle(ref arch);
		}
	}
}