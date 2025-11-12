namespace Connected.Storage.Schemas;

internal sealed class EntitySchema : ISchema
{
	public List<ISchemaColumn> Columns { get; } = [];
	public required string Schema { get; set; }
	public required string Name { get; set; }
	public required string Type { get; set; }
	public bool Ignore { get; set; }

	public bool Equals(ISchema? other)
	{
		if (other is null)
			return false;

		if (!string.Equals(Name, other.Name, StringComparison.Ordinal))
			return false;

		if (!string.Equals(Schema, other.Schema, StringComparison.Ordinal))
			return false;

		if (Columns.Count != other.Columns.Count)
			return false;

		for (var i = 0; i < Columns.Count; i++)
		{
			if (Columns[i] is not IEquatable<ISchemaColumn> left || !left.Equals(other.Columns[i]))
				return false;
		}

		return true;
	}
}