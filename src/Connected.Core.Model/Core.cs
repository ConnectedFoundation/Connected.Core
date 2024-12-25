namespace Connected;

public static class Core
{
	static Core()
	{
		InstanceId = Guid.NewGuid();
	}

	public static Guid InstanceId { get; }
	public static HttpContext? HttpContext => Accessor?.HttpContext;
	internal static IHttpContextAccessor? Accessor { get; set; }
}