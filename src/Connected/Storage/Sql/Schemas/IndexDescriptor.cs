namespace Connected.Storage.Sql.Schemas;

internal class IndexDescriptor
{
	private List<string>? _columns;

	public bool Unique { get; set; }

	public string? Group { get; set; }

	public List<string> Columns => _columns ??= [];

	public override string ToString()
	{
		return string.IsNullOrWhiteSpace(Group) ? Columns[0] : Group;
	}
}
