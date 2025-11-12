namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies that a property should use a special serializer when storing its value
/// and reading it from the storage provider.
/// </summary>
/// <remarks>
/// Creates a new instance of the SerializerAttribute class.
/// </remarks>
/// <param name="type">The serializer type used by a property. 
/// Type should implement IEntityPropertySerializer.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SerializerAttribute(Type type)
		: Attribute
{
	/// <summary>
	/// Gets the serializer type used when reading and writing property value to and from the storage.
	/// </summary>
	public Type Type { get; } = type;
}