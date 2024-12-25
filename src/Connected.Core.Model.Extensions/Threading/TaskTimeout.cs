using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connected.Threading;

public sealed class TaskTimeout : IDisposable
{
	private readonly Func<Task> _pingAction;
	private Task? _pingTask;
	private readonly TimeSpan _pingInterval;

	private readonly Func<Task>? _lifespanAction;
	private Task? _lifespanTask;
	private readonly TimeSpan _lifespan;

	public TaskTimeout(Func<Task> pingAction, TimeSpan pingInterval, Func<Task> lifespanAction, TimeSpan lifespan, CancellationToken cancel)
		: this(pingAction, pingInterval, cancel)
	{
		_lifespanAction = lifespanAction;
		_lifespan = lifespan;
	}
	public TaskTimeout(Func<Task> pingAction, TimeSpan pingInterval, CancellationToken cancel)
	{
		_pingAction = pingAction;
		_pingInterval = pingInterval;

		CancelSource = new CancellationTokenSource();

		cancel.Register(() =>
		{
			CancelSource.Cancel();
		});
	}

	private CancellationTokenSource CancelSource { get; }
	private bool IsRunning { get; set; }

	public void Start()
	{
		if (IsRunning)
			return;

		if (_pingInterval > TimeSpan.Zero)
			_pingTask = Timeout();

		if (_lifespan > TimeSpan.Zero)
			_lifespanTask = Lifespan();

		IsRunning = true;
	}

	public void Stop()
	{
		if (CancelSource.IsCancellationRequested)
			return;

		try
		{
			CancelSource.Cancel();
		}
		catch (OperationCanceledException)
		{

		}
		finally
		{
			IsRunning = false;
		}
	}

	private Task Timeout()
	{
		return Task.Run(async () =>
		{
			try
			{
				while (!CancelSource.IsCancellationRequested || IsRunning)
				{
					if (_pingTask is not null)
					{
						await Task.Delay(_pingInterval, CancelSource.Token).ConfigureAwait(false);
						await _pingAction().ConfigureAwait(false);
					}
				}
			}
			catch (TaskCanceledException)
			{
				/*
				 * Do nothing, it is expected to fire when a timeout is cancelled. 
				 */
			}
			finally
			{
				IsRunning = false;
			}
		}, CancelSource.Token);
	}

	private Task Lifespan()
	{
		return Task.Run(async () =>
		{
			try
			{
				while (!CancelSource.IsCancellationRequested || IsRunning)
				{
					if (_lifespanTask is not null)
					{
						await Task.Delay(_lifespan, CancelSource.Token).ConfigureAwait(false);

						if (_lifespanAction is not null)
							await _lifespanAction().ConfigureAwait(false);
					}
				}
			}
			catch (TaskCanceledException)
			{
				/*
				 * Do nothing, it is expected to fire when a timeout is cancelled. 
				 */
			}
			finally
			{
				IsRunning = false;
			}
		}, CancelSource.Token);
	}

	public void Dispose()
	{
		Stop();
		CancelSource.Dispose();

		if (_pingTask is not null)
		{
			if (_pingTask.IsCompleted)
				_pingTask.Dispose();

			_pingTask = null;
		}

		if (_lifespanTask is not null)
		{
			if (_lifespanTask.IsCompleted)
				_lifespanTask.Dispose();

			_lifespanTask = null;
		}
	}
}
