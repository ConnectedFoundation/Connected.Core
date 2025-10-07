namespace Connected.Collections.Concurrent;

public interface IDispatcherJob<TDto> : IDisposable
{
	Task Invoke(TDto dto, CancellationToken cancel);
}
