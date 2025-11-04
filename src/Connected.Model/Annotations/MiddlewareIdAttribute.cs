namespace Connected.Annotations;

public sealed class MiddlewareIdAttribute(string id)
	: Attribute
{

	public string Id { get; } = id;
}
