//HintName: PositionSystem.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Project
{
	public partial class PositionSystem : ISystem<Position, Position.Vectorized, Position.Array>
	{
		public void Update<T1Arch>(ref ComponentEnumerableNew<Position, Position.Vectorized, Position.Array>.Enumerator<T1Arch> en)
			where T1Arch : unmanaged, IArchType<T1Arch, Position, Position.Vectorized, Position.Array>
		{
			while (en.MoveNext())
			{
				var item = en.Current;
				for (int i = 0; i < 8; i++) // TODO: Compute this value?
				{
					Update(Position.Ref.FromArray(ref item.item1Single, i));
				}
				Update(ref item.item1Vec);
			}
		}
	}
}