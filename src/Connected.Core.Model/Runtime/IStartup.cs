namespace Connected.Runtime;

public interface IStartup
{
	bool IsUpdated { get; }
	void Prepare(IConfiguration configuration);
	void ConfigureServices(IServiceCollection services);
	void Configure(IApplicationBuilder app, IWebHostEnvironment env);

	Task ConfigureEndpoints(IEndpointRouteBuilder builder);
	Task Initialize(IHost host);
	Task Start();
}
