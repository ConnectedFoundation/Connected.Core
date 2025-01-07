namespace Connected.Services;

internal class PatchDto<TPrimaryKey> : PrimaryKeyDto<TPrimaryKey>, IPatchDto<TPrimaryKey>
	where TPrimaryKey : notnull
{
	public PatchDto()
	{
		Properties = [];
	}

	public Dictionary<string, object?> Properties { get; set; }
}
