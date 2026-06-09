namespace Connected.Services;

public class PatchDto<TPrimaryKey> : PrimaryKeyDto<TPrimaryKey>, IPatchDto<TPrimaryKey>
	where TPrimaryKey : notnull
{
	public PatchDto()
	{
		Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
	}

	public Dictionary<string, object?> Properties { get; set; }
}
