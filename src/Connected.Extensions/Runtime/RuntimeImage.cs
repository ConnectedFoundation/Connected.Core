using System.Collections.Immutable;

namespace Connected.Runtime;

public abstract class RuntimeImage : IRuntimeImage
{
	private List<string> Items { get; } = [];

	public IImmutableList<string> Dependencies => Items.ToImmutableList();

	public void Register()
	{
		OnRegister();
	}

	protected virtual void OnRegister()
	{

	}

	protected void RegisterDependency(string assemblyName)
	{
		Items.Add(assemblyName);
	}
}
