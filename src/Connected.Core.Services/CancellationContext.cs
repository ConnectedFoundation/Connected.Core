namespace Connected.Services;

internal sealed class CancellationContext : ICancellationContext
{
	public CancellationContext()
	{
		Source = new();
	}
	private CancellationTokenSource Source { get; }
	public CancellationToken CancellationToken => Source.Token;
	public void Cancel() => Source.Cancel();
}
