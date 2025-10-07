using Connected.Entities;
using System.Reflection;

namespace Connected.Storage;

internal sealed class PropertyContext
{
	public PropertyContext(PropertyInfo property, IEntityPropertySerializer? serializer)
	{
		Property = property;
		Serializer = serializer;
	}

	public PropertyInfo Property { get; }
	public IEntityPropertySerializer? Serializer { get; }
}