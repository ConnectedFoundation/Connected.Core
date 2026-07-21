namespace Connected;

public abstract class Middleware
	: IMiddleware
{
	protected bool IsDisposed { get; private set; }
	protected CancellationToken CancellationToken { get; private set; } = default;
	public async Task Initialize(CancellationToken? cancellationToken = null)
	{
		CancellationToken = cancellationToken ?? CancellationToken.None;

		await OnInitialize();
	}

	protected virtual async Task OnInitialize()
	{
		await Task.CompletedTask;
	}

	protected virtual void OnDisposing(bool disposing)
	{

	}

	protected void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			OnDisposing(disposing);

			IsDisposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
