using Connected.Services;

namespace Connected.Core.Services.Mock;
public class HeadDtoMock<TPrimaryKey>
	: DtoMock, IHeadDto<TPrimaryKey>
{
	public required TPrimaryKey? Head { get; set; }

	public static implicit operator HeadDtoMock<TPrimaryKey>(TPrimaryKey? value)
	{
		return new HeadDtoMock<TPrimaryKey> { Head = value };
	}
}
