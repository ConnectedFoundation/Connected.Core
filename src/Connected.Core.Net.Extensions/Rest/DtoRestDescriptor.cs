namespace Connected.Net.Rest;

internal class DtoRestDescriptor(Type type)
{
	public Type Type { get; } = type;
}
