namespace Connected.Services;

/// <summary>
/// Provides a dictionary of dynamic properties.
/// </summary>
/// <remarks>
/// This interface enables objects to carry additional properties beyond their static
/// type definition. It is commonly used in patch operations to track which properties
/// should be updated, or to pass arbitrary data through service layers.
/// </remarks>
public interface IPropertyProvider
{
	/// <summary>
	/// Gets or sets the dictionary of dynamic properties.
	/// </summary>
	Dictionary<string, object?> Properties { get; set; }
}
