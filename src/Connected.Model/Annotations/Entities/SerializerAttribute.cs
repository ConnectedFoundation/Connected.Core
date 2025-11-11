namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies that a property should use a special serializer when storing its value
/// and reading it from the storage provider.
/// </summary>
public sealed class SerializerAttribute
	: Attribute
{
	/// <summary>
	/// Creates a new instance of the SerializerAttribute class.
	/// </summary>
	/// <param name="type">The serializer type used by a property. 
	/// Type should implement IEntityPropertySerializer.</param>
	public SerializerAttribute(Type type)
	{
		/*
		 * Persist the serializer type so providers can plug-in custom serialization logic
		 * for the annotated property.
		 */
		Type = type;
	}
	/// <summary>
	/// Gets the serializer type used when reading and writing property value to and from the storage.
	/// </summary>
	public Type Type { get; }
}