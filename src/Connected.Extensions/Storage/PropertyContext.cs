using Connected.Entities;
using System.Reflection;

namespace Connected.Storage;

internal sealed class PropertyContext(PropertyInfo property, IEntityPropertySerializer? serializer)
{
	public PropertyInfo Property { get; } = property;
	public IEntityPropertySerializer? Serializer { get; } = serializer;
}