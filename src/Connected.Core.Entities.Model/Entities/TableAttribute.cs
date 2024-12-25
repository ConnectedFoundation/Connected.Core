namespace Connected.Annotations.Entities;
/// <summary>
/// Defines how an Entity is created in physical storage.
/// </summary>
/// <remarks>
/// By decorating an Entity with this attribute we instructs the Connected that it should
/// create a database table when creating schema.
/// All Entities must have this attribute set if a physical storage is used. Currently, only tables
/// are supported so this is the only attribute that can be set on Entities regarding storage schemas.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
public class TableAttribute : SchemaAttribute
{
	/// <summary>
	/// Creates a new instance of the TableAttribute class.
	/// </summary>
	public TableAttribute()
	{

	}
	/// <summary>
	/// Creates a new instance of the TableAttribute class.
	/// </summary>
	/// <param name="schema">The name of the schema which acts as a category under which the entity will be accessible.</param>
	public TableAttribute(string schema)
	{
		Schema = schema;
	}
}
