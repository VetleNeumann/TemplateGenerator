using LightParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TemplateGenerator
{
	public class ModelParameter : IParameter<ReturnType>
	{
		IModel<ReturnType> value;

		public ModelParameter(IModel<ReturnType> value)
		{
			this.value = value;
		}

		public bool TryGet<T>(out IEnumerable<T> value)
		{
			if (typeof(T) == typeof(IParameter<ReturnType>) || typeof(IParameter<ReturnType>).IsAssignableFrom(typeof(T)))
			{
				var enumerable = this.value.GetEnumerable().Select(x => x.Value);
				value = Unsafe.As<IEnumerable<IParameter<ReturnType>>, IEnumerable<T>>(ref enumerable);
				return true;
			}


			value = default;
			return false;
		}

		public bool TryGet<T>(out T value)
		{
			if (typeof(T) == typeof(IModel<ReturnType>) || typeof(IModel<ReturnType>).IsAssignableFrom(typeof(T)))
			{
				value = Unsafe.As<IModel<ReturnType>, T>(ref this.value);
				return true;
			}

			value = default;
			return false;
		}

		public bool TryGet(ReadOnlySpan<char> name, out IParameter<ReturnType> parameter)
		{
			return value.TryGet(name, out parameter);
		}

		public bool TrySet<T>(T value)
		{
			if (typeof(T) == typeof(IModel<ReturnType>) || typeof(IModel<ReturnType>).IsAssignableFrom(typeof(T)))
			{
				this.value = Unsafe.As<T, IModel<ReturnType>>(ref value);
				return true;
			}

			return false;
		}

		ReturnType IParameter<ReturnType>.GetType()
		{
			return ReturnType.Model;
		}
	}

	public class EnumerableParameter<T> : IParameter<ReturnType>
	{
		IEnumerable<T> value;

		public EnumerableParameter(IEnumerable<T> value)
		{
			this.value = value;
		}

		public bool TryGet<T1>(out IEnumerable<T1> value)
		{
			if (typeof(T1) == typeof(T) || typeof(T).IsAssignableFrom(typeof(T1)))
			{
				value = Unsafe.As<IEnumerable<T>, IEnumerable<T1>>(ref this.value);
				return true;
			}

			value = default;
			return false;
		}

		public bool TryGet<T1>(out T1 value)
		{
			if (typeof(T1) == typeof(IEnumerable<T>) || typeof(IEnumerable<T>).IsAssignableFrom(typeof(T1)))
			{
				value = Unsafe.As<IEnumerable<T>, T1>(ref this.value);
				return true;
			}

			value = default;
			return false;
		}

		public bool TryGet(ReadOnlySpan<char> name, out IParameter<ReturnType> parameter)
		{
			parameter = default;
			return false;
		}

		public bool TrySet<T1>(T1 value)
		{
			if (typeof(T1) == typeof(IEnumerable<T>) || typeof(IEnumerable<T>).IsAssignableFrom(typeof(T1)))
			{
				this.value = Unsafe.As<T1, IEnumerable<T>>(ref value);
				return true;
			}

			return false;
		}

		ReturnType IParameter<ReturnType>.GetType()
		{
			return ReturnType.Enumerable;
		}
	}
}
