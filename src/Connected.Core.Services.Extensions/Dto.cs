namespace Connected.Services;

public class Dto : IDto
{
	public static IDto Empty => new Dto();

	public static IDto Factory => new Dto();
}
