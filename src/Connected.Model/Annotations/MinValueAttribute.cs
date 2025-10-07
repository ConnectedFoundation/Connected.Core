using System.ComponentModel.DataAnnotations;

namespace Connected.Annotations;

public class MinValueAttribute : ValidationAttribute
{
	public double Value { get; }

	public MinValueAttribute(double value) => Value = value;

	public override bool IsValid(object? value)
	{
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

	public override string FormatErrorMessage(string name)
	{
		return FormatMessage(name, Value.ToString());
	}

	public static string FormatMessage(string name, object value)
	{
		return $"{string.Format(Strings.ValMinValue, value)} ({name})";
	}
}