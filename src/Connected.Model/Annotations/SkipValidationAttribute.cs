namespace Connected.Annotations;
/// <summary>
/// Marks a property so that built-in validation logic skips evaluating the decorated member.
/// </summary>
/// <remarks>
/// Useful for properties whose values are computed or validated externally and should not
/// participate in standard validation passes.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class SkipValidationAttribute
	: Attribute
{
	/*
	 * Marker attribute with no state. Validation pipelines check for its presence to bypass
	 * default or custom validators for the associated property.
	 */
}
