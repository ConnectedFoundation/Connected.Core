using Connected.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Connected.Services;

internal class ValueDto<T> : Dto, IValueDto<T>
{
	public ValueDto()
	{
	}

	public ValueDto(T value)
	{
		Value = value;
	}

	public T Value { get; set; } = default!;

	public static implicit operator ValueDto<T>(T value)
	{
		return new ValueDto<T> { Value = value };
	}
}
