namespace Connected.Workers;

/// <summary>
/// Represents a background worker that executes scheduled or continuous operations.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IHostedService"/> to integrate with the application's
/// hosted service lifecycle management. Workers are long-running background tasks that can
/// perform operations such as scheduled jobs, data processing, monitoring, or any other
/// continuous or periodic work. The hosting infrastructure manages the worker's startup
/// and shutdown in coordination with the application's lifecycle, ensuring proper resource
/// management and graceful termination.
/// </remarks>
public interface IWorker
	: IHostedService
{
}
