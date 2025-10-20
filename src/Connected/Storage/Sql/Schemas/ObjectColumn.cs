namespace Connected.Storage.Sql.Schemas;

internal class ObjectColumn
{
	public required string Name { get; set; }
	public required string Type { get; set; }
	public bool Computed { get; set; }
	public int Length { get; set; }
	public int Precision { get; set; }
	public int Scale { get; set; }
	public bool Nullable { get; set; }
	public string? TrimTrailingBlanks { get; set; }
	public string? FixedLenInSource { get; set; }
	public string? Collation { get; set; }
}
