using Microsoft.Extensions.DependencyInjection;

namespace Connected;
/// <summary>
/// Event arguments carrying metadata about a discovered dependency type during startup.
/// </summary>
/// <remarks>
/// Provides access to the discovered <see cref="Type"/> and the active <see cref="IServiceCollection"/>
/// so handlers can register services or perform additional configuration in response to discovery events.
/// </remarks>
/// <param name="services">The service collection available at discovery time.</param>
/// <param name="type">The discovered dependency type.</param>
public class DependencyTypeDiscoveryEventArgs(IServiceCollection services, Type type)
	: EventArgs
{
	/// <summary>
	/// Gets the discovered dependency type.
	/// </summary>
	public Type Type { get; } = type;
	/// <summary>
	/// Gets the service collection available when the type was discovered.
	/// </summary>
	public IServiceCollection Services { get; } = services;
}
