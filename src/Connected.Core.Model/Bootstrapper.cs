namespace Connected;

internal sealed class Bootstrapper : Runtime.IStartup
{
	public bool IsUpdated => false;

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddLogging(builder => builder.AddConsole());
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		Core.Accessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
	}

	public async Task ConfigureEndpoints(IEndpointRouteBuilder builder)
	{
		await Task.CompletedTask;
	}

	public async Task Initialize(IHost host)
	{
		await Task.CompletedTask;
	}

	public async Task Start()
	{
		await Task.CompletedTask;
	}
}
