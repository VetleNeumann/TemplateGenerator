using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using TemplateGenerator;

namespace EnCS.Generator
{
	static class ComponentGeneratorDiagnostics
	{
		public static readonly DiagnosticDescriptor InvalidComponentMemberType = new("ECS001", "Invalid component member type", "Component member of type '{0}' is not supported", "ComponentGenerator", DiagnosticSeverity.Error, true);
		
		public static readonly DiagnosticDescriptor ComponentMustBePartial = new("ECS002", "Component struct must be partial", "Component struct is not partial", "ComponentGenerator", DiagnosticSeverity.Error, true);
	}

	class ComponentGenerator : ITemplateSourceGenerator<StructDeclarationSyntax>
	{
		const int MAX_SIMD_BUFFER_BITS = 512;
		const int ARRAY_ELEMENTS = 8;
		const int BITS_PER_BYTE = 8;

		public string Template => ResourceReader.GetResource("Component.tcs");

		public bool TryCreateModel(Compilation compilation, StructDeclarationSyntax node, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			model = new Model<ReturnType>();

			if (!IsValidComponent(node, diagnostics))
				return false;

			model.Set("namespace".AsSpan(), new Parameter<string>(node.GetNamespace()));
			model.Set("compName".AsSpan(), new Parameter<string>(node.Identifier.ToString()));
			model.Set("arraySize".AsSpan(), new Parameter<float>(ARRAY_ELEMENTS));

			var membersResult = TryGetMembers(node, diagnostics, out var members);
			model.Set("members".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(members.Select(x => x.GetModel())));

			return membersResult;
		}

		public bool Filter(StructDeclarationSyntax node)
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

		public string GetName(StructDeclarationSyntax node)
		{
			return node.Identifier.ToString();
		}

		public static bool IsValidComponent(StructDeclarationSyntax node, List<Diagnostic> diagnostics)
		{
			bool hasAttribute = node.AttributeLists.SelectMany(x => x.Attributes).Select(x => x.Name as SimpleNameSyntax).Any(x => x.Identifier.Text == "ComponentAttribute" || x.Identifier.Text == "Component");
			bool hasProperties = node.Members.Any(x => x is PropertyDeclarationSyntax);

			bool fieldsValid = true;
			foreach (var member in node.Members.Where(x => x is FieldDeclarationSyntax).Select(x => x as FieldDeclarationSyntax))
			{
				if (!TryGetTypeName(member.Declaration.Type, diagnostics, out string typeName))
					fieldsValid = false;

				if (!TryGetTypeSize(member.Declaration.Type, diagnostics, out int size))
					fieldsValid = false;
			}

			return hasAttribute && !hasProperties && fieldsValid;
			//return node.Modifiers.Any(x => x.Value == "partial");
		}

		static bool TryGetMembers(StructDeclarationSyntax node, List<Diagnostic> diagnostics, out List<ComponentMember> members)
		{
			members = new List<ComponentMember>();

			foreach (var member in node.Members.Where(x => x is FieldDeclarationSyntax).Select(x => x as FieldDeclarationSyntax))
			{
				if (!TryGetTypeName(member.Declaration.Type, diagnostics, out string typeName))
					continue;

				if (!TryGetTypeSize(member.Declaration.Type, diagnostics, out int size))
					continue;

				int bitsPerVector = Math.Min(size * BITS_PER_BYTE * ARRAY_ELEMENTS, MAX_SIMD_BUFFER_BITS);
				int vectorArraySize = (size * BITS_PER_BYTE * ARRAY_ELEMENTS) / MAX_SIMD_BUFFER_BITS;

				members.Add(new ComponentMember()
				{
					name = member.Declaration.Variables[0].ToString(),
					type = typeName,
					bits = bitsPerVector,
					arraySize = vectorArraySize
				});
			}

			return members.Count > 0;
		}

		static bool TryGetTypeName<T>(T type, List<Diagnostic> diagnostics, out string name) where T : TypeSyntax
		{
			if (type is PredefinedTypeSyntax predefined)
			{
				name = predefined.Keyword.Text;
				return true;
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
						if (!TryGetTypeName(arg, diagnostics, out var nestedName))
						{
							name = "";
							return false;
						}

						sb.Append(nestedName);
					}
					sb.Append('>');
				}

				name = sb.ToString();
				return true;
			}
			else
			{
				diagnostics.Add(Diagnostic.Create(ComponentGeneratorDiagnostics.InvalidComponentMemberType, type.GetLocation(), type.ToString()));
				name = "";
				return false;
			}
		}

		static bool TryGetTypeSize<T>(T type, List<Diagnostic> diagnostics, out int size) where T : TypeSyntax
		{
			if (type is PredefinedTypeSyntax predefined)
			{
				if (!TryGetSize(predefined.Keyword.Text, out size))
				{
					diagnostics.Add(Diagnostic.Create(ComponentGeneratorDiagnostics.InvalidComponentMemberType, type.GetLocation(), type.ToString()));
					return false;
				}

				return true;
			}
			else if (type is GenericNameSyntax generic)
			{
				int nameLength = generic.Identifier.Text.Length;

				if ((nameLength == 11 || nameLength == 12) && generic.Identifier.Text.StartsWith("FixedArray"))
				{
					int arrayLength = int.Parse(generic.Identifier.Text.Substring(10));

					if (!TryGetTypeName(generic.TypeArgumentList.Arguments[0], diagnostics, out string typeName))
					{
						size = 0;
						return false;
					}

					if (!TryGetSize(typeName, out int typeSize))
					{
						size = 0;
						return false;
					}

					size = arrayLength * typeSize;
					return true;
				}

				diagnostics.Add(Diagnostic.Create(ComponentGeneratorDiagnostics.InvalidComponentMemberType, type.GetLocation(), type.ToString()));

				size = 0;
				return false;
			}
			else
			{
				diagnostics.Add(Diagnostic.Create(ComponentGeneratorDiagnostics.InvalidComponentMemberType, type.GetLocation(), type.ToString()));

				size = 0;
				return false;
			}
		}

		static bool TryGetSize(string numericTypeName, out int size)
		{
			switch (numericTypeName)
			{
				case "sbyte":
				case "byte":
				case "char":
					size = 1;
					return true;
				case "short":
				case "ushort":
					size = 2;
					return true;
				case "int":
				case "uint":
					size = 4;
					return true;
				case "long":
				case "ulong":
					size = 8;
					return true;
				case "float":
					size = 4;
					return true;
				case "double":
					size = 8;
					return true;
				case "decimal":
					size = 16;
					return true;
				default:
					size = 0;
					return false;
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

	struct ComponentMember
	{
		public string name;
		public string type;
		public int bits;
		public int arraySize;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("name".AsSpan(), Parameter.Create(name));
			model.Set("type".AsSpan(), Parameter.Create(type));
			model.Set("bits".AsSpan(), Parameter.Create<float>(bits));
			model.Set("arraySize".AsSpan(), Parameter.Create<float>(arraySize));

			return model;
		}
	}
}