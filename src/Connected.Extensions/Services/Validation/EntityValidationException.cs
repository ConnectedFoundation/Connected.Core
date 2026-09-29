using System.ComponentModel.DataAnnotations;

namespace Connected.Services.Validation;

/// <summary>
/// What a validation refused.
/// </summary>
public enum ValidationFailure
{
	/// <summary>A value that must be unique already exists.</summary>
	Duplicate = 0,
	/// <summary>A value refers to something that does not exist.</summary>
	NotFound = 1,
	/// <summary>Something cannot go while other things still refer to it.</summary>
	Referenced = 2,
	/// <summary>A value contains a character it may not.</summary>
	InvalidCharacter = 3,
	/// <summary>Something is disabled.</summary>
	Disabled = 4,
	/// <summary>A value does not match what it has to.</summary>
	Mismatch = 5,
	/// <summary>A user is not valid.</summary>
	InvalidUser = 6,
	/// <summary>The caller may not do this.</summary>
	Unauthorized = 7,
	/// <summary>A value is required.</summary>
	ValueExpected = 8
}

/// <summary>
/// A validation failure that says, besides its message, what was refused and about what.
/// </summary>
/// <remarks>
/// The message is exactly what <see cref="ValidationExceptions"/> always produced, so nothing reading the text changes.
/// What is new is that a caller wanting to explain the failure in its own terms - an import telling a plant which cell
/// is wrong - no longer has to take that text apart, which stops working the moment the text is translated.
/// </remarks>
public sealed class EntityValidationException(ValidationFailure failure, string message) : ValidationException(message)
{
	/// <summary>Gets what was refused.</summary>
	public ValidationFailure Failure { get; } = failure;

	/// <summary>Gets the kind of thing the failure is about, where it is known.</summary>
	public Type? Entity { get; init; }

	/// <summary>Gets the kind of thing still referring to <see cref="Entity"/>, for <see cref="ValidationFailure.Referenced"/>.</summary>
	public Type? ReferencingEntity { get; init; }

	/// <summary>Gets the argument the failure is about, where the caller named one rather than a type.</summary>
	public string? Argument { get; init; }

	/// <summary>Gets the value that was refused, where there is one.</summary>
	public object? Value { get; init; }
}

/// <summary>
/// An entity that had to exist and did not.
/// </summary>
/// <remarks>
/// Still a <see cref="NullReferenceException"/> with the same message, so every existing catch behaves as before; it
/// adds the entity type the lookup was for.
/// </remarks>
public sealed class EntityExpectedException(Type entity, string message) : NullReferenceException(message)
{
	/// <summary>Gets the kind of entity that was expected.</summary>
	public Type Entity { get; } = entity;
}
