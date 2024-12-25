namespace Connected.Services;

public interface ICancellationContext
{
	CancellationToken CancellationToken { get; }
}