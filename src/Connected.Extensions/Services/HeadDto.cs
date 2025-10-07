namespace Connected.Services;

internal class HeadDto<T> : Dto, IHeadDto<T>
{
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
