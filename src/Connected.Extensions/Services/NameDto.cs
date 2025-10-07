namespace Connected.Services;

internal class NameDto : Dto, INameDto
{
	public NameDto()
	{

	}

	public NameDto(string name)
	{
		Name = name;
	}

	public string? Name { get; set; }

	public static implicit operator NameDto(string value)
	{
		return new NameDto { Name = value };
	}
}
