namespace Connected.Storage.Schemas;

public interface ISchema : IEquatable<ISchema>
{
	List<ISchemaColumn> Columns { get; }

	string? Schema { get; }
	string? Name { get; }
	string? Type { get; }
	bool Ignore { get; }
}
