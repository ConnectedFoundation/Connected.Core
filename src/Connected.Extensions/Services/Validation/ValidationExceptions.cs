using System.ComponentModel.DataAnnotations;

namespace Connected.Services.Validation;

public static class ValidationExceptions
{
	public static ValidationException ValueExists(string argument, object? value)
	{
		var valueString = value is null ? "null" : value;

		return new ValidationException($"{Strings.ValDuplicate} ({argument}, {valueString})");
	}

	public static ValidationException InvalidCharacter(string argument, char value)
	{
		return new ValidationException($"{Strings.ValInvalidChars} ({argument}, {value})");
	}

	public static ValidationException NotFound(string argument, object? value)
	{
		var valueString = value is null ? "null" : value;

		return new ValidationException($"{Strings.ValNotFound} ({argument}, {valueString})");
	}

	public static ValidationException Disabled(string argument)
	{
		return new ValidationException($"{Strings.ValEntityDisabled} ({argument})");
	}

	public static ValidationException Disabled(string argument, object value)
	{
		return new ValidationException($"{Strings.ValEntityDisabled} ({argument}:{value})");
	}

	public static ValidationException ReferenceExists(Type entity, object value)
	{
		return new ValidationException($"{Strings.ValReference} ({entity.Name}, {value})");
	}

	public static ValidationException Mismatch(string argument, object value)
	{
		return new ValidationException($"{Strings.ValMismatch} ({argument}, {value})");
	}

	public static ValidationException InvalidUser(string argument)
	{
		return new ValidationException($"{Strings.ValInvalidUser} ({argument})");
	}

	public static ValidationException Unauthorized()
	{
		return new ValidationException(Strings.ValUnauthorized);
	}

	public static ValidationException ValueExpected(string argument)
	{
		return new ValidationException($"{Strings.ValValueExpected} ({argument})");
	}
}
