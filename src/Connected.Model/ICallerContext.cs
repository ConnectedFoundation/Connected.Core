namespace Connected;

public interface ICallerContext
{
	object? Sender { get; }
	string? Method { get; }
}
