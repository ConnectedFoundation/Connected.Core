using System.ComponentModel.DataAnnotations;

namespace Connected.Annotations;
/// <summary>
/// Validation attribute indicating whether antiforgery validation should be enforced for the decorated class.
/// </summary>
/// <remarks>
/// When <see cref="ValidateRequest"/> is true, consumers can trigger antiforgery token checks in their
/// request handling pipeline; when false, the request may bypass antiforgery validation.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class ValidateAntiforgeryAttribute(bool validateRequest = false)
	: ValidationAttribute
{
	/*
	 * Primary constructor captures desired antiforgery enforcement flag. The attribute itself does not
	 * perform validation logic here; consuming middleware or filters can inspect the flag to decide
	 * whether to execute antiforgery token verification.
	 */
	/// <summary>
	/// Gets a value indicating whether antiforgery validation should be performed.
	/// </summary>
	/// <remarks>
	/// True signals that antiforgery tokens must be validated; false allows the request to proceed without
	/// antiforgery checks.
	/// </remarks>
	public bool ValidateRequest { get; } = validateRequest;
}
