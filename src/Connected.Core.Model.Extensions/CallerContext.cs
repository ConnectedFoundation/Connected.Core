namespace Connected;
public class CallerContext : ICallerContext
{
	public object? Sender { get; set; }

	public string? Method { get; set; }
}
