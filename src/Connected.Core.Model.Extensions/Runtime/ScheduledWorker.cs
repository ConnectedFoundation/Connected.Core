using System.Threading.Tasks;
using System.Threading;
using System;

namespace Connected.Workers;

public abstract class ScheduledWorker : Worker
{
	private Timer? _timer = null;
	private Task? _executingTask = null;
	private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

	protected virtual TimeSpan Timer { get; set; } = TimeSpan.FromMinutes(1);
	protected long Count { get; private set; }

	public override Task StartAsync(CancellationToken stoppingToken)
	{
		stoppingToken.Register(() =>
		{
			_stoppingCts.Cancel();
		});

		_timer = new Timer(OnTimer, null, TimeSpan.Zero, Timer);

		return Task.CompletedTask;
	}

	private void OnTimer(object? state)
	{
		_timer?.Change(Timeout.Infinite, Timeout.Infinite);
		_executingTask = ExecuteTaskAsync(_stoppingCts.Token);
	}

	private async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		try
		{
			await OnInvoke(stoppingToken);
		}
		finally
		{
			_timer?.Change(Timer, Timeout.InfiniteTimeSpan);
		}
	}

	public sealed override async Task StopAsync(CancellationToken cancellationToken)
	{
		_timer?.Change(Timeout.Infinite, 0);

		if (_executingTask is null)
			return;

		try
		{
			_stoppingCts.Cancel();
		}
		finally
		{
			await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
		}
	}

	protected override void OnDisposing()
	{
		_stoppingCts.Cancel();
		_timer?.Dispose();
	}
}