using Connected.Services;

namespace Connected.Core.Services.Mock;
public class PatchDtoMock<TPrimaryKey> : PrimaryKeyDtoMock<TPrimaryKey>, IPatchDto<TPrimaryKey>
	where TPrimaryKey : notnull
{
	public required Dictionary<string, object?> Properties { get; set; }
}
