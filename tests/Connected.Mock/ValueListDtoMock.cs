using Connected.Services;

namespace Connected.Core.Services.Mock;
public class ValueListDtoMock<TValue>
	: DtoMock, IPrimaryKeyListDto<TValue>
{
	public ValueListDtoMock()
	{

	}

	public ValueListDtoMock(params TValue[] items)
	{
		Items = [];

		foreach (var item in items)
			Items.Add(item);
	}
	public List<TValue> Items { get; set; } = [];
}
