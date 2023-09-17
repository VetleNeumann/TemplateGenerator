//HintName: VelocitySystem.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using EnCS;

namespace Project.Primitives
{
	public partial class VelocitySystem
	{
		public void Update<T1Arch>(ref ComponentEnumerableNew<Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array, Project.Primitives.Velocity, Project.Primitives.Velocity.Vectorized, Project.Primitives.Velocity.Array>.Enumerator<T1Arch> en)
			where T1Arch : unmanaged, IArchType<T1Arch, Project.Primitives.Position, Project.Primitives.Position.Vectorized, Project.Primitives.Position.Array>, IArchType<T1Arch, Project.Primitives.Velocity, Project.Primitives.Velocity.Vectorized, Project.Primitives.Velocity.Array>
		{
			while (en.MoveNext())
			{
				var item = en.Current;
				for (int i = 0; i < 8; i++) // TODO: Compute this value?
				{
					Update(Project.Primitives.Position.Ref.FromArray(ref item.item1Single, i), Project.Primitives.Velocity.Ref.FromArray(ref item.item2Single, i));
				}
				Update(ref item.item1Vec, ref item.item2Vec);
			}
		}
	}
}