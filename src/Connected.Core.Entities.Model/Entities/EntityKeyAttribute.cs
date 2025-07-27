namespace Connected.Annotations.Entities;

[AttributeUsage(AttributeTargets.Class)]
public sealed class EntityKeyAttribute(string key)
  : Attribute
{
	public string Key { get; } = key;
}
