using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Connected.Annotations;
/// <summary>
/// Validation attribute ensuring a value differs from the type's default value.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NonDefaultAttribute
	: ValidationAttribute
{
	/// <summary>
	/// Formats the validation error message for the specified member name.
	/// </summary>
	/// <param name="name">Member name.</param>
	/// <returns>Error message string.</returns>
	public override string FormatErrorMessage(string name)
	{
		/*
		 * Delegate to resource-based message indicating a non-default requirement.
		 */
		return $"'{name}' {Strings.ValNonDefault}";
	}
	/// <summary>
	/// Validates that the supplied value is not the default for its type.
	/// </summary>
	/// <param name="value">Value to validate.</param>
	/// <returns>True when non-default; otherwise false.</returns>
	public override bool IsValid(object? value)
	{
		/*
		 * Null values always invalid. For value types, create instance to compare; for ref types,
		 * default is null so prior check suffices.
		 */
		if (value is null)
			return false;
		var defaultValue = value.GetType().IsClass || value.GetType().IsInterface ? null : Activator.CreateInstance(value.GetType());
		return Comparer.DefaultInvariant.Compare(value, defaultValue) != 0;
	}
}