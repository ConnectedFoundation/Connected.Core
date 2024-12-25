namespace Connected.Annotations;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ValidateRequestAttribute : Attribute
{
	public ValidateRequestAttribute(bool validateRequest)
	{
		ValidateRequest = validateRequest;
	}

	public bool ValidateRequest { get; }
}
