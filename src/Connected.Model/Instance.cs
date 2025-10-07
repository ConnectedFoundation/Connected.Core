namespace Connected;

public static class Instance
{
	static Instance()
	{
		Id = Guid.NewGuid();
	}

	public static Guid Id { get; }
}