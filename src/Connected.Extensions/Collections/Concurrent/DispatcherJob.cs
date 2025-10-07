using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Collections.Concurrent;

/// <summary>
/// This class acts as a job unit of the <see cref="IDispatcher{T}"/>.
/// </summary>
/// <typeparam name="TArgs"></typeparam>
public abstract class DispatcherJob<TDto> : IDispatcherJob<TDto>, IDisposable
{
	internal AsyncServiceScope? Scope { get; set; }
	protected bool IsDisposed { get; private set; }
	protected CancellationToken Cancel { get; private set; }
	protected TDto Dto { get; private set; } = default!;

	public async Task Invoke(TDto dto, CancellationToken cancel)
	{
		Cancel = cancel;
		Dto = dto;

		try
		{
			await OnInvoke();
		}
		catch (Exception ex)
		{
			await HandleException(ex);
		}
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}

	private async Task HandleException(Exception ex)
	{
		await Scope.Rollback();
		await OnHandleEception(ex);
	}

	protected virtual async Task OnHandleEception(Exception ex)
	{
		await Task.CompletedTask;
	}

	protected object? GetService(Type serviceType)
	{
		if (Scope is null)
			return null;

		return Scope.Value.ServiceProvider.GetService(serviceType);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (IsDisposed)
			return;

		if (disposing)
			OnDisposing(disposing);

		IsDisposed = true;
	}

	protected virtual void OnDisposing(bool disposing)
	{

	}
}