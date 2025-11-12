namespace Connected.Membership.Annotations;

public enum ClaimsOptions
{
	Any = 1,
	All = 2
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ClaimsAttribute(string claims, ClaimsOptions options = ClaimsOptions.Any)
	: Attribute
{
	public ClaimsOptions Options { get; } = options;
	public string Claims { get; } = claims;
}
