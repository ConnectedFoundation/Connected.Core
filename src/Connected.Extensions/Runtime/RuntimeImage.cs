namespace Connected.Runtime;

public abstract class RuntimeImage : IRuntimeImage
{
	public void Register()
	{
		OnRegister();
	}

	protected virtual void OnRegister()
	{

	}
}
