using Connected.Services;

namespace Connected.Core.Services.Mock;
public class TagListDtoMock
	: DtoMock, ITagListDto
{
	public TagListDtoMock()
	{

	}

	public TagListDtoMock(params string[] items)
	{
		Items = [];

		foreach (var item in items)
			Items.Add(item);
	}
	public List<string> Items { get; set; } = [];
}
