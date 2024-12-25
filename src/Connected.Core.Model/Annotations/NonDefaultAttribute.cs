using System.ComponentModel.DataAnnotations;

namespace Connected.Annotations;

[AttributeUsage(AttributeTargets.Property)]
public sealed class NonDefaultAttribute : ValidationAttribute
{
	public override string FormatErrorMessage(string name)
	{
		return $"'{name}' {Strings.ValNonDefault}";
	}

	public override bool IsValid(object? value)
	{
		if (value is null)
			return false;

		var defaultValue = value.GetType().IsClass || value.GetType().IsInterface ? null : Activator.CreateInstance(value.GetType());

		return Comparer.DefaultInvariant.Compare(value, defaultValue) == 0;
	}
}