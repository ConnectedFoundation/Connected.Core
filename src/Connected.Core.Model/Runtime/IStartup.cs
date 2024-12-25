namespace Connected.Runtime;

public interface IStartup
{
	void ConfigureServices(IServiceCollection services);
	void Configure(IApplicationBuilder app, IWebHostEnvironment env);

	Task ConfigureEndpoints(IEndpointRouteBuilder builder);
	Task Initialize(IHost host);
	Task Start();
	bool IsUpdated { get; }
}
