namespace Connected.Collections.Concurrent;
/// <summary>
/// Represents a unit of work capable of processing dispatcher arguments of type <typeparamref name="TDto"/>.
/// </summary>
/// <typeparam name="TDto">The argument type.</typeparam>
/// <remarks>
/// Dispatcher jobs are invoked by a dispatcher to handle queued items. Implementations should honor
/// the provided cancellation token and perform minimal blocking to keep throughput high.
/// </remarks>
public interface IDispatcherJob<TDto>
	: IDisposable
{
	/// <summary>
	/// Executes the job logic for the supplied argument, observing cancellation requests.
	/// </summary>
	/// <param name="dto">The argument value to process.</param>
	/// <param name="cancel">A token used to signal cancellation.</param>
	Task Invoke(TDto dto, CancellationToken cancel);
}
