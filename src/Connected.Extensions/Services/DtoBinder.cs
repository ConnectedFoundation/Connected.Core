namespace Connected.Services;

public abstract class DtoBinder : IDtoBinder
{
	protected object? Instance { get; private set; }

	public void Invoke(object instance, params object[] arguments)
	{
		Instance = instance;

		OnInvoke(arguments);
	}

	protected virtual void OnInvoke(params object[] arguments)
	{

	}
}