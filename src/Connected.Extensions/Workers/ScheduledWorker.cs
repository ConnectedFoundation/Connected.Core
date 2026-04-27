namespace Connected.Workers;
/// <summary>
/// Provides an abstract base class for workers that execute on a periodic schedule using async/await pattern.
/// </summary>
/// <remarks>
/// ScheduledWorker implements a background task that repeatedly invokes <see cref="Worker.OnInvoke"/> at regular intervals
/// defined by the <see cref="Timer"/> property. The implementation uses Task.Delay for scheduling rather than System.Threading.Timer,
/// avoiding dedicated timer threads and using the thread pool more efficiently.
/// Execution behavior:
/// - Invokes immediately upon start
/// - Waits for Timer duration between invocations
/// - Continues execution until cancellation is requested
/// - Gracefully handles cancellation during execution or delay
/// Derived classes should override OnInvoke to implement the periodic work. Exceptions in OnInvoke are caught and logged
/// but do not prevent subsequent invocations from being scheduled.
/// </remarks>
public abstract class ScheduledWorker : Worker
{
	private Task? _executingTask = null;
	private readonly CancellationTokenSource _stoppingCts = new();

	/// <summary>
	/// Gets or sets the interval between invocations of the worker.
	/// </summary>
	/// <remarks>
	/// This property defines how long to wait after each invocation completes before starting the next one.
	/// Default value is 1 minute. Changes to this property only take effect before StartAsync is called.
	/// </remarks>
	protected virtual TimeSpan Timer { get; set; } = TimeSpan.FromMinutes(1);

	/// <summary>
	/// Gets the number of times the worker has successfully completed an invocation.
	/// </summary>
	/// <remarks>
	/// This counter increments after each successful completion of OnInvoke. It does not increment
	/// if OnInvoke throws an exception.
	/// </remarks>
	protected long Count { get; private set; }

	/// <summary>
	/// Starts the scheduled worker and begins the periodic execution loop.
	/// </summary>
	/// <param name="stoppingToken">The cancellation token to signal shutdown.</param>
	/// <returns>A completed task since the execution loop runs in the background.</returns>
	/// <remarks>
	/// This method registers the stopping token for cancellation propagation and starts the background
	/// execution loop. The loop begins immediately with the first invocation.
	/// </remarks>
	public override async Task StartAsync(CancellationToken stoppingToken)
	{
		/*
		 * Register the stopping token to propagate cancellation to our internal cancellation source.
		 */
		stoppingToken.Register(_stoppingCts.Cancel);
		/*
		 * Start the background execution loop.
		 * This task runs continuously until cancellation is requested.
		 */
		_executingTask = ExecuteAsync(_stoppingCts.Token);

		await Task.CompletedTask;
	}

	/// <summary>
	/// Executes the periodic invocation loop using async/await pattern.
	/// </summary>
	/// <param name="stoppingToken">The cancellation token to signal shutdown.</param>
	/// <returns>A task that completes when the loop exits due to cancellation.</returns>
	/// <remarks>
	/// This method implements the scheduling loop:
	/// 1. Invoke OnInvoke immediately
	/// 2. Wait for Timer duration using Task.Delay
	/// 3. Repeat until cancellation is requested
	/// Exceptions in OnInvoke are caught to prevent the loop from terminating, ensuring subsequent
	/// invocations continue to be scheduled.
	/// </remarks>
	private async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		/*
		 * Continue executing until cancellation is requested.
		 */
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				/*
				 * Invoke the worker's periodic task.
				 */
				await OnInvoke(stoppingToken);

				/*
				 * Increment the successful invocation counter.
				 */
				Count++;
			}
			catch
			{
				/*
				 * Swallow exceptions from OnInvoke to keep the scheduler running.
				 * Derived classes should handle and log their own exceptions appropriately.
				 */
			}

			/*
			 * Check for cancellation before attempting the delay to exit quickly during shutdown.
			 */
			if (stoppingToken.IsCancellationRequested)
				break;

			try
			{
				/*
				 * Wait for the scheduled interval before the next invocation.
				 * Task.Delay throws OperationCanceledException when the token is cancelled.
				 */
				await Task.Delay(Timer, stoppingToken);
			}
			catch (OperationCanceledException)
			{
				/*
				 * Cancellation requested during delay - exit the loop gracefully.
				 */
				break;
			}
		}
	}

	/// <summary>
	/// Stops the scheduled worker and waits for the current invocation to complete.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token that limits how long to wait for graceful shutdown.</param>
	/// <returns>A task that completes when the worker has stopped or the cancellation token is cancelled.</returns>
	/// <remarks>
	/// This method signals cancellation to the execution loop and waits for either the loop to exit or
	/// the provided cancellation token to be cancelled. This enables graceful shutdown with a timeout.
	/// </remarks>
	public sealed override async Task StopAsync(CancellationToken cancellationToken)
	{
		/*
		 * If the execution task was never started, nothing to stop.
		 */
		if (_executingTask is null)
			return;

		try
		{
			/*
			 * Signal the execution loop to stop.
			 */
			_stoppingCts.Cancel();
		}
		finally
		{
			/*
			 * Wait for either the execution task to complete or the cancellation token to be cancelled.
			 * This allows for graceful shutdown with a timeout.
			 */
			await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
		}
	}

	/// <summary>
	/// Performs cleanup when the worker is being disposed.
	/// </summary>
	/// <remarks>
	/// This method signals cancellation to ensure the execution loop terminates if still running.
	/// </remarks>
	protected override void OnDisposing()
	{
		/*
		 * Signal cancellation to stop the execution loop.
		 */
		_stoppingCts.Cancel();
	}
}