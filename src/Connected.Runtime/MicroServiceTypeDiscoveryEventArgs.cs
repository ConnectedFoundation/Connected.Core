using Microsoft.Extensions.DependencyInjection;

namespace Connected;
public class MicroServiceTypeDiscoveryEventArgs(IServiceCollection services, Type type)
	: EventArgs
{
	public Type Type { get; } = type;
	public IServiceCollection Services { get; } = services;
}
