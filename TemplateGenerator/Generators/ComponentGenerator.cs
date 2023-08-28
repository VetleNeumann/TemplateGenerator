using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TemplateGenerator
{
	class ComponentGenerator : ITemplateSourceGenerator<StructDeclarationSyntax>
	{
		const int MAX_SIMD_BUFFER_BITS = 512;

		public string Template => "Component.tcs";

		public bool Filter(GeneratorSyntaxContext context, StructDeclarationSyntax node)
		{
			foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if ((attributeSyntax.Name as SimpleNameSyntax).Identifier.Text == "ComponentAttribute")
						return true;

					if ((attributeSyntax.Name as SimpleNameSyntax).Identifier.Text == "Component")
						return true;
				}
			}

			return false;
		}

		public Model<ReturnType> CreateModel(Compilation compilation, StructDeclarationSyntax node)
		{
			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), new Parameter<string>(TemplateGeneratorHelpers.GetNamespace(node)));
			model.Set("name".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("members".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(GetMembers(node)));

			return model;
		}

		public string GetName(StructDeclarationSyntax node)
		{
			return node.Identifier.ToString();
		}

		static Model<ReturnType>[] GetMembers(StructDeclarationSyntax node)
		{
			var models = new List<Model<ReturnType>>();

			foreach (var member in node.Members.Where(x => x is FieldDeclarationSyntax).Select(x => x as FieldDeclarationSyntax))
			{
				string typeName = GetTypeName(member.Declaration.Type);
				int size = GetTypeSize(member.Declaration.Type);

				int bits = Math.Min(size * 8 * 8, MAX_SIMD_BUFFER_BITS);
				int arraySize = (size * 8 * 8) / MAX_SIMD_BUFFER_BITS;

				var model = new Model<ReturnType>();
				model.Set("name".AsSpan(), Parameter.Create(member.Declaration.Variables[0].ToString()));
				model.Set("type".AsSpan(), Parameter.Create(typeName));
				model.Set("bits".AsSpan(), Parameter.Create<float>(bits));
				model.Set("arraySize".AsSpan(), Parameter.Create<float>(arraySize));

				models.Add(model);
			}

			return models.ToArray();
		}

		static string GetTypeName<T>(T type) where T : TypeSyntax
		{
			if (type is PredefinedTypeSyntax predefined)
			{
				return predefined.Keyword.Text;
			}
			else if (type is GenericNameSyntax generic)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(generic.Identifier.Text);

				if (generic.TypeArgumentList.Arguments.Count > 0)
				{
					sb.Append('<');
					foreach (TypeSyntax arg in generic.TypeArgumentList.Arguments)
					{
						sb.Append(GetTypeName(arg));
					}
					sb.Append('>');
				}

				return sb.ToString();
			}
			else
			{
				throw new Exception("Unexpected type declaration");
			}
		}

		static int GetTypeSize<T>(T type) where T : TypeSyntax
		{
			if (type is PredefinedTypeSyntax predefined)
			{
				return GetSize(predefined.Keyword.Text);
			}
			else if (type is GenericNameSyntax generic)
			{
				int nameLength = generic.Identifier.Text.Length;

				if ((nameLength == 11 || nameLength == 12) && generic.Identifier.Text.StartsWith("FixedArray"))
				{
					int arrayLength = int.Parse(generic.Identifier.Text.Substring(10));
					int typeSize = GetSize(GetTypeName(generic.TypeArgumentList.Arguments[0]));

					return arrayLength * typeSize;
				}

				throw new Exception("Unexpected type declaration");
			}
			else
			{
				throw new Exception("Unexpected type declaration");
			}
		}

		static int GetSize(string numericTypeName)
		{
			switch (numericTypeName)
			{
				case "sbyte":
				case "byte":
				case "char":
					return 1;
				case "short":
				case "ushort":
					return 2;
				case "int":
				case "uint":
					return 4;
				case "long":
				case "ulong":
					return 8;
				case "float":
					return 4;
				case "double":
					return 8;
				case "decimal":
					return 16;
				default:
					throw new Exception($"Invalid numeric type '{numericTypeName}'");
			}
		}

		static bool IsFixedNumericType(string name)
		{
			switch (name)
			{
				case "sbyte":
				case "byte":
				case "char":
				case "short":
				case "ushort":
				case "int":
				case "uint":
				case "long":
				case "ulong":
				case "float":
				case "double":
				case "decimal":
					return true;
				default:
					return false;

			}
		}
	}
}