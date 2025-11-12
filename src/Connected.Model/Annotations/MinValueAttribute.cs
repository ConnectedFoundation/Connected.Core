using System.ComponentModel.DataAnnotations;

namespace Connected.Annotations;
/// <summary>
/// Validation attribute ensuring a numeric value is greater than or equal to a configured minimum.
/// </summary>
/// <remarks>
/// Initializes the attribute with the minimum allowed value.
/// </remarks>
/// <param name="value">The inclusive lower bound.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MinValueAttribute(double value)
		: ValidationAttribute
{
	/// <summary>
	/// Gets the minimum allowed numeric value.
	/// </summary>
	public double Value { get; } = value;

	/// <summary>
	/// Determines whether the specified value satisfies the minimum constraint.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <returns>True if valid; otherwise false.</returns>
	public override bool IsValid(object? value)
	{
		/*
		 * Attempt to convert to double; if conversion fails or comparison fails, validation fails.
		 */
		try
		{
			var dv = Convert.ToDouble(value);
			return dv >= Value;
		}
		catch
		{
			return false;
		}
	}
	/// <summary>
	/// Formats an error message using the member name and configured minimum.
	/// </summary>
	/// <param name="name">The member name.</param>
	/// <returns>Error message string.</returns>
	public override string FormatErrorMessage(string name)
	{
		return FormatMessage(name, Value.ToString());
	}
	/// <summary>
	/// Helper to format the error message including the minimum value.
	/// </summary>
	/// <param name="name">The member name.</param>
	/// <param name="value">The minimum value.</param>
	/// <returns>Formatted error message.</returns>
	public static string FormatMessage(string name, object value)
	{
		/*
		 * Compose localized validation string with the affected member name.
		 */
		return $"{string.Format(Strings.ValMinValue, value)} ({name})";
	}
}