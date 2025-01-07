namespace Connected.Services;

internal class PrimaryKeyDto<T> : Dto, IPrimaryKeyDto<T>
{
	public PrimaryKeyDto() { }

	public PrimaryKeyDto(T id)
	{
		Id = id;
	}

	public T? Id { get; set; }

	public static implicit operator PrimaryKeyDto<T?>(T value)
	{
		return new PrimaryKeyDto<T?> { Id = value };
	}
}
