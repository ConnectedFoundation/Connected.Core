using Connected.Services;

namespace Connected.Core.Services.Mock;
public class PrimaryKeyDtoMock<TPrimaryKey> : DtoMock, IPrimaryKeyDto<TPrimaryKey>
{
	public required TPrimaryKey Id { get; set; }
}
