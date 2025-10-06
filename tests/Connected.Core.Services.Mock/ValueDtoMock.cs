using Connected.Services;

namespace Connected.Core.Services.Mock;
public class ValueDtoMock<TValue> : DtoMock, IValueDto<TValue>
{
	public required TValue Value { get; set; }

	public static implicit operator ValueDtoMock<TValue>(TValue value)
	{
		return new ValueDtoMock<TValue> { Value = value };
	}
}
