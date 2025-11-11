namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies if the property acts as a primary key in the storage.
/// </summary>
/// <remarks>
/// Creates a new instance of the PrimaryKeyAttribute class.
/// </remarks>
/// <param name="isIdentity">Specifies wether the property should use an automatically assigned unique 
/// value when inserted.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PrimaryKeyAttribute(bool isIdentity)
		: Attribute
{
	/// <summary>
	/// Get a value indicating is the property should have an automatically assigned value when inserting.
	/// </summary>
	public bool IsIdentity { get; } = isIdentity;
}
