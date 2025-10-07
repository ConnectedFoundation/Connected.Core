using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Hosting;

namespace Connected.Runtime;
/// <summary>
/// This component acts as a background worker.
/// </summary>
public abstract class Worker : IHostedService, IWorker, IDisposable
{
	/*
	 * We use IHostedService instead of BackgroundService because of:
	 * - https://github.com/dotnet/runtime/issues/36063
	 */
	public virtual async Task StartAsync(CancellationToken stoppingToken)
	{
		try
		{
			await OnInvoke(stoppingToken);
		}
		catch (Exception ex)
		{
			await OnError(ex);
		}
	}

	public virtual async Task StopAsync(CancellationToken stoppingToken)
	{
		await Task.CompletedTask;
	}

	protected virtual async Task OnInvoke(CancellationToken cancellationToken)
	{
		await Task.CompletedTask;
	}

	protected virtual async Task OnError(Exception ex)
	{
		await Task.CompletedTask;
	}

	public void Dispose()
	{

	}

	protected virtual void OnDisposing()
	{
		
	}
}
