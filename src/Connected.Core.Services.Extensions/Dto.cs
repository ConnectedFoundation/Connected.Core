namespace Connected.Services;

public class Dto : IDto
{
	public const int DefaultCodeLength = 128;
	public const int DefaultNameLength = 128;
	public const int DefaultTitleLength = 128;
	public const int DefaultTagsLength = 1024;
	public static IDto Empty => new Dto();
	public static IDto Factory => new Dto();
}
