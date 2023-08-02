//HintName: PositionSystem.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Project
{
	public partial struct PositionSystem : ISystem<Position, Position.Vectorized, Position.Array>
	{
		public void Update<TArch1>(ref ArchTypeEnumerable<TArch1, Position, Position.Vectorized, Position.Array> loop)
			where TArch1 : unmanaged, IArchType<TArch1, Position, Position.Vectorized, Position.Array>
		{
			var en = loop.GetEnumerator();
			while (en.MoveNext())
			{
				var item = en.Current;
				for (int i = 0; i < 8; i++) // TODO: Compute this value?
				{
					Update(Position.FromArray(ref item.item1Single, i));
				}
				Update(ref item.item1Vec);
			}
		}

		public void Update<TArch1, TArch2>(ref ArchTypeEnumerable<TArch1, Position, Position.Vectorized, Position.Array> loop)
			where TArch1 : unmanaged, IArchType<TArch1, Position, Position.Vectorized, Position.Array>
			where TArch2 : unmanaged, IArchType<TArch2, Position, Position.Vectorized, Position.Array>
		{
			var en = loop.GetEnumerator();
			while (en.MoveNext())
			{
				var item = en.Current;
				for (int i = 0; i < 8; i++) // TODO: Compute this value?
				{
					Update(Position.FromArray(ref item.item1Single, i));
				}
				Update(ref item.item1Vec);
			}
		}
	}
}