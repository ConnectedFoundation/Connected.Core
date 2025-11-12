namespace Connected.Services;

public class Dto : IDto
{
	public const int DefaultCodeLength = 128;
	public const int DefaultNameLength = 128;
	public const int DefaultTitleLength = 128;
	public const int DefaultTagsLength = 1024;
	public const int DefaultIdentityLength = 128;

	public const int DefaultDistributedHeadLength = 257;

	public const int MaxLength = -1;
	public static IDto Empty => new Dto();
	public static IDto Factory => new Dto();
}

public class Dto<TDto> : Dto
	where TDto : IDto
{
	public Dto()
	{
		Value = Factory.Create<TDto>();
	}
	public TDto Value { get; }
}
