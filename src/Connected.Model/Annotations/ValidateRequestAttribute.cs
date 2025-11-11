namespace Connected.Annotations;
/// <summary>
/// Indicates whether a request associated with the decorated property should be validated by the pipeline.
/// </summary>
/// <param name="validateRequest">True to enable request validation; false to bypass it.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ValidateRequestAttribute(bool validateRequest)
	: Attribute
{
	/*
	 * Marker attribute carrying a boolean flag that consumers (middleware/validators) can read
	 * to decide if request validation must occur for the associated member.
	 */
	/// <summary>
	/// Gets a value indicating whether validation should be performed for the request.
	/// </summary>
	public bool ValidateRequest { get; } = validateRequest;
}
