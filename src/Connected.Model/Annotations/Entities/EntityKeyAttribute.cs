namespace Connected.Annotations.Entities;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class EntityKeyAttribute(string key)
  : Attribute
{
	/// <summary>
	/// Gets the unique key assigned to the Entity interface.
	/// </summary>
	/// <remarks>
	/// Captures a stable identifier for the Entity interface so systems can reference
	/// or register the Entity by a value decoupled from the CLR type name.
	/// </remarks>
	public string Key { get; } = key;
}
