//HintName: PositionSystem.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Project.Primitives
{
	public partial class PositionSystem
	{
		public void Update<T1Arch>(ref ComponentEnumerableNew<Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>.Enumerator<T1Arch> en)
			where T1Arch : unmanaged, IArchType<T1Arch, Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>
		{
			while (en.MoveNext())
			{
				var item = en.Current;
				var remaining = en.Remaining;
				for (int i = 0; i < remaining; i++)
				{
					Update(Project.Primitives.Position.Ref.FromArray(ref item.item1Single, i));
				}
				Update(ref item.item1Vec);
				for (int i = 0; i < remaining; i++)
				{
					UpdateAfter(Project.Primitives.Position.Ref.FromArray(ref item.item1Single, i));
				}
			}
		}
	}
}