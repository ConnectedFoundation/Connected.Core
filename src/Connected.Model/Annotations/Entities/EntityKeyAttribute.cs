namespace Connected.Annotations.Entities;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class EntityKeyAttribute(string key)
  : Attribute
{
	public string Key { get; } = key;
}
