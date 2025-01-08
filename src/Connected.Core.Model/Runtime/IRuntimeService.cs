using Connected.Annotations;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Runtime;

[Flags]
public enum StartOptions
{
	None = 0,
	SynchronizeSchemas = 1
}

public enum Stage
{
	Development = 1,
	QualityAssurance = 2,
	Staging = 3,
	Production = 4
}

public enum Platform
{
	OnPrem = 1,
	Cloud = 2
}

public enum Connectivity
{
	Online = 1,
	Offline = 2
}

public enum Optimization
{
	Debug = 1,
	Release = 2
}

[Service(ServiceRegistrationScope.Singleton)]
public interface IRuntimeService
{
	Task<ImmutableList<Assembly>> QueryMicroServices();
	Task<ImmutableList<IStartup>> QueryStartups();
	Task<ImmutableList<Assembly>> QueryUpdatedMicroServices();
	Task<StartOptions> SelectStartOptions();
	Task<Platform> SelectPlatform();
	Task<Connectivity> SelectConnectivity();
	Task<Optimization> SelectOptimization();
	Task<ImmutableList<Type>> QueryMiddlewares();
}
