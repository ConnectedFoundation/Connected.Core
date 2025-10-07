namespace Connected.Services;

public interface IDtoBinder
{
	void Invoke(object instance, params object[] arguments);
}