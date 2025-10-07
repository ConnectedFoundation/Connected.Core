using Connected.Services;

namespace Connected.Core.Services.Mock;
public class NameDtoMock : DtoMock, INameDto
{
	public required string? Name { get; set; }

	public static implicit operator NameDtoMock(string? value)
	{
		return new NameDtoMock { Name = value };
	}
}
