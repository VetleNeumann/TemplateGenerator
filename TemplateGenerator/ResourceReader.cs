using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace TemplateGenerator
{
	public class ResourceReader
	{
		public static string GetResource<TAssembly>(string endWith) => GetResource(endWith, typeof(TAssembly));

		public static string GetResource(string endWith, Type assemblyType = null)
		{
			var assemblies = GetAssemblies();
			var assembly = assemblies.Where(x => x.GetManifestResourceNames().Where(r => r.EndsWith(endWith)).Any());

			if (assembly.Count() > 1)
				throw new InvalidOperationException($"There is more then one assembly with a resource that ends with '{endWith}'");

			var resources = assembly.Single().GetManifestResourceNames().Where(r => r.EndsWith(endWith));

			if (!resources.Any())
				throw new InvalidOperationException($"There is no resources that ends with '{endWith}'");

			if (resources.Count() > 1)
				throw new InvalidOperationException($"There is more then one resource that ends with '{endWith}'");

			var resourceName = resources.Single();

			return ReadEmbededResource(assembly.Single(), resourceName);
		}

		private static IEnumerable<Assembly> GetAssemblies()
		{
			return AppDomain.CurrentDomain.GetAssemblies();
		}

		private static string ReadEmbededResource(Assembly assembly, string name)
		{
			using (var resourceStream = assembly.GetManifestResourceStream(name))
			{
				if (resourceStream == null) return null;

				using (var streamReader = new StreamReader(resourceStream))
				{
					return streamReader.ReadToEnd();
				}
			}
		}
	}
}
