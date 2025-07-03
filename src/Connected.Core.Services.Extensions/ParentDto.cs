namespace Connected.Services;

internal class ParentDto<T> : Dto, IParentDto<T>
{
	public T? Parent { get; set; }

	public static implicit operator ParentDto<T?>(T value)
	{
		return new ParentDto<T?> { Parent = value };
	}

	public static implicit operator ParentDto<T>(PrimaryKeyDto<T> value)
	{
		return new ParentDto<T> { Parent = value.Id };
	}
}
