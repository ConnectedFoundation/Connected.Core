using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
