using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Configuration.Environment;

public interface IEnvironmentService
{
	ImmutableList<Assembly> MicroServices { get; }
	IEnvironmentServices Services { get; }
}