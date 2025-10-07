using Connected.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Connected.Services;

internal class PrimaryKeyDto<T> : Dto, IPrimaryKeyDto<T>
{
	public PrimaryKeyDto()
	{

	}

	public PrimaryKeyDto(T id)
	{
		Id = id;
	}

	[Required, NonDefault]
	public T Id { get; set; } = default!;

	public static implicit operator PrimaryKeyDto<T>(T value)
	{
		return new PrimaryKeyDto<T> { Id = value };
	}
}
