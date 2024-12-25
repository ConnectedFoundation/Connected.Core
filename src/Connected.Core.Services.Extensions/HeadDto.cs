namespace Connected.Services;

public class HeadDto<T> : Dto, IHeadDto<T>
{
	public HeadDto() { }

	public HeadDto(T head)
	{
		this.Head = head;
	}

	public T? Head { get; set; }

	public static implicit operator HeadDto<T?>(T value)
	{
		return new HeadDto<T?> { Head = value };
	}

	public static implicit operator HeadDto<T>(PrimaryKeyDto<T> value)
	{
		return new HeadDto<T> { Head = value.Id };
	}
}
