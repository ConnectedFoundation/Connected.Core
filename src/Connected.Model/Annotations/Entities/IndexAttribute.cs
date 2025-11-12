namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies that a property should be indexed by a storage provider.
/// </summary>
/// <remarks>
/// Creates a new instance of the IndexAttribute class.
/// </remarks>
/// <param name="unique">Specifies if the index should be unique which means that a value
/// cannot repeat in the entire record set</param>
/// <param name="name">An optional name of the index. If more that one property share the
/// same index name a storage provider should create a combine index. Note that storage
/// providers don't guarantee that is will create index with the exact name.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IndexAttribute(bool unique, string? name = null)
		: Attribute
{
	/// <summary>
	/// Gets a value which indicates if the index is unique.
	/// </summary>
	public bool Unique { get; set; } = unique;
	/// <summary>
	/// Gets the name of the index. If this value is null, storage provider will automatically
	/// create an index name.
	/// </summary>
	public string? Name { get; set; } = name;
}
