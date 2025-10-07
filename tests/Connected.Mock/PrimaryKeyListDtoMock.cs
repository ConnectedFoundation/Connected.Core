using Connected.Services;

namespace Connected.Core.Services.Mock;
public class PrimaryKeyListDtoMock<TPrimaryKey>
	: DtoMock, IPrimaryKeyListDto<TPrimaryKey>
{
	public PrimaryKeyListDtoMock()
	{

	}

	public PrimaryKeyListDtoMock(params TPrimaryKey[] items)
	{
		Items = [];

		foreach (var item in items)
			Items.Add(item);
	}
	public List<TPrimaryKey> Items { get; set; } = [];
}
