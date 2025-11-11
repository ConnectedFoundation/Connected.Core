namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies a default value that should be used by storage providers if no value
/// is passed when inserting records.
/// </summary>
/// <remarks>
/// This value is also used if the Entity is added on existing schema which means that
/// a storage provider must add a new field. If the field does not allows null values this
/// attribute is a must because the sycnhronization would otherwise fail.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DefaultAttribute
	: Attribute
{
	/// <summary>
	/// Creates a new instance of the Default class.
	/// </summary>
	/// <param name="value">The value that should be used as default value.</param>
	public DefaultAttribute(object? value)
	{
		/*
		 * Store the provided default value so schema generators and updaters can apply
		 * it to the backing store for missing values on insert.
		 */
		Value = value;
	}
	/// <summary>
	/// Gets the default value that should be used by storage provider if no value for the property is specified.
	/// </summary>
	public object? Value { get; }
}
