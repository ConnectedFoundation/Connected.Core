namespace Connected;

public abstract class Middleware : IMiddleware
{
	protected bool IsDisposed { get; private set; }

	public async Task Initialize()
	{
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
