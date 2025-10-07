using Microsoft.Extensions.DependencyInjection;

namespace Connected;
public class DependencyTypeDiscoveryEventArgs(IServiceCollection services, Type type)
	: EventArgs
{
	public Type Type { get; } = type;
	public IServiceCollection Services { get; } = services;
}
