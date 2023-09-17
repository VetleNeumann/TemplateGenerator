//HintName: Position.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Project.Primitives
{
	public partial struct Position : IComponent<Position, Position.Vectorized, Position.Array>
	{
		public struct Vectorized
		{
			public Vector256<int> x;
			public Vector256<int> y;
			public FixedArray2<Vector512<FixedArray4<int>>> z;
		}

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public struct Array
		{
			public const int Size = 8;

			public FixedArray8<int> x;
			public FixedArray8<int> y;
			public FixedArray8<FixedArray4<int>> z;
		}

		public ref struct Ref
		{
			public ref int x;
			public ref int y;
			public ref FixedArray4<int> z;
			
			public Ref(ref int x, ref int y, ref FixedArray4<int> z)
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
		public static ref Vectorized GetVec<TArch>(ref TArch arch) where TArch : unmanaged, IArchType<TArch, Position, Vectorized, Array>
		{
			return ref TArch.GetVec(ref arch);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref Array GetSingle<TArch>(ref TArch arch) where TArch : unmanaged, IArchType<TArch, Position, Vectorized, Array>
		{
			return ref TArch.GetSingle(ref arch);
		}
	}
}