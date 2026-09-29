using System.ComponentModel.DataAnnotations;

namespace Connected.Services.Validation;

public static class ValidationExceptions
{
	[Obsolete("use generic overload instead", error: false)]
	public static ValidationException ValueExists(string argument, object? value)
	{
		var valueString = value is null ? "null" : value;

		return new EntityValidationException(ValidationFailure.Duplicate, $"{Strings.ValDuplicate} ({argument}, {valueString})")
		{
			Argument = argument,
			Value = value
		};
	}

	public static ValidationException ValueExists<TEntity>(object? value)
	{
		var valueString = value is null ? "null" : value;

		return new EntityValidationException(ValidationFailure.Duplicate, $"{Strings.ValDuplicate} ({typeof(TEntity).Name}, {valueString})")
		{
			Entity = typeof(TEntity),
			Value = value
		};
	}

	public static ValidationException InvalidCharacter(string argument, char value)
	{
		return new EntityValidationException(ValidationFailure.InvalidCharacter, $"{Strings.ValInvalidChars} ({argument}, {value})")
		{
			Argument = argument,
			Value = value
		};
	}

	[Obsolete("use generic overload instead", error: false)]
	public static ValidationException NotFound(string argument, object? value)
	{
		var valueString = value is null ? "null" : value;

		return new EntityValidationException(ValidationFailure.NotFound, $"{Strings.ValNotFound} ({argument}, {valueString})")
		{
			Argument = argument,
			Value = value
		};
	}

	public static ValidationException NotFound<TEntity>(object? value)
	{
		var valueString = value is null ? "null" : value;

		return new EntityValidationException(ValidationFailure.NotFound, $"{Strings.ValNotFound} ({typeof(TEntity).Name}, {valueString})")
		{
			Entity = typeof(TEntity),
			Value = value
		};
	}

	public static ValidationException Disabled(string argument)
	{
		return new EntityValidationException(ValidationFailure.Disabled, $"{Strings.ValEntityDisabled} ({argument})")
		{
			Argument = argument
		};
	}

	public static ValidationException Disabled(string argument, object value)
	{
		return new EntityValidationException(ValidationFailure.Disabled, $"{Strings.ValEntityDisabled} ({argument}:{value})")
		{
			Argument = argument,
			Value = value
		};
	}

	[Obsolete("use generic overload instead", error: false)]
	public static ValidationException ReferenceExists(Type entity, object value)
	{
		return new EntityValidationException(ValidationFailure.Referenced, $"{Strings.ValReference} ({entity.Name}, {value})")
		{
			Entity = entity,
			Value = value
		};
	}

	public static ValidationException ReferenceExists<TReferencedEntity, TReferencingEntity>(object value)
	{
		return new EntityValidationException(ValidationFailure.Referenced,
			$"{Strings.ValReference} ({typeof(TReferencedEntity).Name}, {typeof(TReferencingEntity).Name}, {value})")
		{
			Entity = typeof(TReferencedEntity),
			ReferencingEntity = typeof(TReferencingEntity),
			Value = value
		};
	}

	public static ValidationException Mismatch(string argument, object value)
	{
		return new EntityValidationException(ValidationFailure.Mismatch, $"{Strings.ValMismatch} ({argument}, {value})")
		{
			Argument = argument,
			Value = value
		};
	}

	public static ValidationException InvalidUser(string argument)
	{
		return new EntityValidationException(ValidationFailure.InvalidUser, $"{Strings.ValInvalidUser} ({argument})")
		{
			Argument = argument
		};
	}

	public static ValidationException Unauthorized()
	{
		return new EntityValidationException(ValidationFailure.Unauthorized, Strings.ValUnauthorized);
	}

	public static ValidationException ValueExpected(string argument)
	{
		return new EntityValidationException(ValidationFailure.ValueExpected, $"{Strings.ValValueExpected} ({argument})")
		{
			Argument = argument
		};
	}
}
