﻿namespace Connected.Runtime;
public abstract class Startup : IStartup
{
	public bool IsUpdated { get; }
	protected IHost? Host { get; private set; }
	protected IServiceProvider? ServiceProvider => Host?.Services;
	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		OnConfigure(app, env);
	}

	protected virtual void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{

	}

	public void ConfigureServices(IServiceCollection services)
	{
		OnConfigureServices(services);
	}

	protected virtual void OnConfigureServices(IServiceCollection services)
	{

	}

	public async Task ConfigureEndpoints(IEndpointRouteBuilder builder)
	{
		await OnConfigureEndpoints(builder);
	}

	protected virtual async Task OnConfigureEndpoints(IEndpointRouteBuilder builder)
	{
		await Task.CompletedTask;
	}

	public async Task Initialize(IHost host)
	{
		Host = host;

		await OnInitialize();
	}

	protected virtual async Task OnInitialize()
	{
		await Task.CompletedTask;
	}

	public async Task Start()
	{
		await OnStart();
	}

	protected virtual async Task OnStart()
	{
		await Task.CompletedTask;
	}

	public async Task ConfigureEndpoints()
	{
		await OnConfigureEndpoints();
	}

	protected virtual Task OnConfigureEndpoints()
	{
		return Task.CompletedTask;
	}
}
