using Connected.Net.Rest.OpenApi.Documentation;
using Connected.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Connected.Net.Rest.OpenApi.Generation;

/// <summary>
/// Builds <see cref="OpenApiSchema"/> instances from CLR types via reflection, registering
/// complex (Dto) types as reusable components keyed by type so recursive graphs terminate.
/// </summary>
internal sealed class OpenApiSchemaBuilder(IDictionary<string, OpenApiSchema> components)
{
	private readonly Dictionary<Type, OpenApiSchema> _references = [];

	public OpenApiSchema Build(Type type)
	{
		type = Nullable.GetUnderlyingType(type) ?? type;

		if (TryBuildPrimitive(type, out var primitive))
			return primitive;

		if (TryBuildEnumerable(type, out var array))
			return array;

		return BuildReference(type);
	}

	private static bool TryBuildPrimitive(Type type, out OpenApiSchema schema)
	{
		if (type == typeof(string) || type == typeof(char))
		{
			schema = new OpenApiSchema { Type = "string" };
			return true;
		}

		if (type == typeof(Guid))
		{
			schema = new OpenApiSchema { Type = "string", Format = "uuid" };
			return true;
		}

		if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
		{
			schema = new OpenApiSchema { Type = "string", Format = "date-time" };
			return true;
		}

		if (type == typeof(TimeSpan))
		{
			schema = new OpenApiSchema { Type = "string", Format = "duration" };
			return true;
		}

		if (type == typeof(bool))
		{
			schema = new OpenApiSchema { Type = "boolean" };
			return true;
		}

		if (type.IsEnum)
		{
			schema = new OpenApiSchema
			{
				Type = "string",
				Enum = [.. Enum.GetNames(type).Select(f => (IOpenApiAny)new OpenApiString(f))]
			};
			return true;
		}

		if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint))
		{
			schema = new OpenApiSchema { Type = "integer", Format = "int32" };
			return true;
		}

		if (type == typeof(long) || type == typeof(ulong))
		{
			schema = new OpenApiSchema { Type = "integer", Format = "int64" };
			return true;
		}

		if (type == typeof(float))
		{
			schema = new OpenApiSchema { Type = "number", Format = "float" };
			return true;
		}

		if (type == typeof(double) || type == typeof(decimal))
		{
			schema = new OpenApiSchema { Type = "number", Format = "double" };
			return true;
		}

		if (type == typeof(object) || type == typeof(void))
		{
			schema = new OpenApiSchema { Type = "object" };
			return true;
		}

		schema = null!;
		return false;
	}

	private bool TryBuildEnumerable(Type type, out OpenApiSchema schema)
	{
		if (type != typeof(string) && type.IsArray && type.GetElementType() is Type elementType)
		{
			schema = new OpenApiSchema { Type = "array", Items = Build(elementType) };
			return true;
		}

		if (type != typeof(string) && type.IsGenericType)
		{
			var enumerableInterface = new[] { type }.Concat(type.GetInterfaces())
				.FirstOrDefault(f => f.IsGenericType && f.GetGenericTypeDefinition() == typeof(IEnumerable<>));

			if (enumerableInterface is not null)
			{
				schema = new OpenApiSchema { Type = "array", Items = Build(enumerableInterface.GetGenericArguments()[0]) };
				return true;
			}
		}

		if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
		{
			schema = new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "object" } };
			return true;
		}

		schema = null!;
		return false;
	}

	private OpenApiSchema BuildReference(Type type)
	{
		if (_references.TryGetValue(type, out var existing))
			return existing;

		var id = SchemaId(type);
		var reference = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = id } };

		/*
		 * Register the reference before walking properties so that a Dto graph referencing
		 * itself (directly or transitively) terminates instead of recursing forever.
		 */
		_references[type] = reference;

		var schema = new OpenApiSchema
		{
			Type = "object",
			Properties = new Dictionary<string, OpenApiSchema>(),
			Description = XmlDocumentationProvider.GetSummary(type)
		};

		components[id] = schema;

		foreach (var property in type.GetInheritedProperites().DistinctBy(f => f.Name))
		{
			if (property.GetIndexParameters().Length > 0)
				continue;

			var propertySchema = Build(property.PropertyType);

			ApplyPropertyConstraints(property, propertySchema, schema);

			schema.Properties[property.Name.ToCamelCase()] = propertySchema;
		}

		return reference;
	}

	private static void ApplyPropertyConstraints(PropertyInfo property, OpenApiSchema propertySchema, OpenApiSchema owner)
	{
		/*
		 * OpenAPI 3.0 treats $ref as an exclusive keyword, so constraints only apply to
		 * schemas built inline (primitives, enums, arrays) rather than component references.
		 */
		if (propertySchema.Reference is null)
		{
			propertySchema.Nullable = property.IsNullable();
			propertySchema.Description = XmlDocumentationProvider.GetSummary(property);

			if (property.FindAttribute<MaxLengthAttribute>() is MaxLengthAttribute maxLength && maxLength.Length > 0)
				propertySchema.MaxLength = maxLength.Length;

			if (property.FindAttribute<StringLengthAttribute>() is StringLengthAttribute stringLength)
				propertySchema.MaxLength = stringLength.MaximumLength;
		}

		if (property.FindAttribute<RequiredAttribute>() is not null)
			owner.Required.Add(property.Name.ToCamelCase());
	}

	private static string SchemaId(Type type)
	{
		if (!type.IsGenericType)
			return type.ToFriendlyName();

		return $"{type.Name.Split('`')[0]}Of{string.Join('_', type.GetGenericArguments().Select(SchemaId))}";
	}
}
