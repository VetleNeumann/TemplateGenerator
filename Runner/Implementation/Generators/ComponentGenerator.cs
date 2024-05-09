using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

	struct ComponentGeneratorData : IEquatable<ComponentGeneratorData>
	{
		public StructDeclarationSyntax node;
		public Compilation compilation;

		public EquatableArray<ComponentMember> members;

		public ComponentGeneratorData(StructDeclarationSyntax node, Compilation compilation, EquatableArray<ComponentMember> members)
		{
			this.node = node;
			this.compilation = compilation;
			this.members = members;
		}

		public bool Equals(ComponentGeneratorData other)
		{
			return members.Equals(other.members);
		}
	}

	class ComponentGenerator : ITemplateSourceGenerator<StructDeclarationSyntax, ComponentGeneratorData>
	{
		const int MAX_SIMD_BUFFER_BITS = 512;
		const int ARRAY_ELEMENTS = 8;
		const int BITS_PER_BYTE = 8;

		public string Template => ResourceReader.GetResource("Component.tcs");

		public bool TryCreateModel(ComponentGeneratorData data, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			model = new Model<ReturnType>();

			model.Set("namespace".AsSpan(), new Parameter<string>(data.node.GetNamespace()));
			model.Set("compName".AsSpan(), new Parameter<string>(data.node.Identifier.ToString()));
			model.Set("arraySize".AsSpan(), new Parameter<float>(ARRAY_ELEMENTS));

			//var membersResult = TryGetMembers(compilation, node, diagnostics, out var members);
			model.Set("members".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(data.members.Select(x => x.GetModel())));

			return true;
		}

		public ComponentGeneratorData? Filter(StructDeclarationSyntax node, SemanticModel semanticModel)
		{
			if (semanticModel.GetDeclaredSymbol(node) is not INamedTypeSymbol typeSymbol)
				return null;

			if (!TryGetMembers(typeSymbol, out var members))
				return null;

			return new ComponentGeneratorData(node, semanticModel.Compilation, new(members.ToArray()));
		}

		public string GetName(ComponentGeneratorData data)
			=> data.node.Identifier.ToString();

		public Location GetLocation(ComponentGeneratorData data)
			=> data.node.GetLocation();

		static bool TryGetMembers(INamedTypeSymbol comp, out List<ComponentMember> members)
		{
			members = new List<ComponentMember>();

			bool hasProperties = false;
			foreach (var member in comp.GetMembers())
			{
				if (member is IPropertySymbol)
					hasProperties = true;

				if (member is not IFieldSymbol field)
					continue;

				if (!TryGetTypeName(field.Type, out string typeName))
					continue;

				if (!TryGetTypeSize(field.Type, out int size))
					continue;

				int bitsPerVector = Math.Min(size * BITS_PER_BYTE * ARRAY_ELEMENTS, MAX_SIMD_BUFFER_BITS);
				int vectorArraySize = (size * BITS_PER_BYTE * ARRAY_ELEMENTS) / MAX_SIMD_BUFFER_BITS;

				members.Add(new ComponentMember()
				{
					name = member.Name,
					type = typeName,
					bits = bitsPerVector,
					arraySize = vectorArraySize
				});
			}

			return !hasProperties && members.Count > 0;
		}

		static bool TryGetTypeName(ITypeSymbol type, out string name)
		{
			name = type.Name;
			return true;
		}

		static bool TryGetTypeSize(ITypeSymbol type, out int size)
		{
			if (type.IsUnmanagedType)
				return TryGetSize(type.Name, out size);

			size = 0;
			if (type is not INamedTypeSymbol namedType)
				return false;

			switch (namedType.TypeKind)
			{
				case TypeKind.Enum:
				{
					return TryGetTypeSize(namedType.EnumUnderlyingType, out size);
				}
				default:
				{
					//diagnostics.Add(Diagnostic.Create(ComponentGeneratorDiagnostics.InvalidComponentMemberType, type.GetLocation(), type.ToString()));
					return false;
				}
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

	struct ComponentMember : IEquatable<ComponentMember>
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

		public bool Equals(ComponentMember other)
		{
			return name.Equals(other.name)
				&& type.Equals(other.type)
				&& bits.Equals(other.bits)
				&& arraySize.Equals(other.arraySize);
		}
	}
}