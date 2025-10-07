namespace Connected;

/// <summary>
/// This is markup interface which should implement all middleware interfaces.
/// </summary>
public interface IMiddleware : IDisposable
{
	Task Initialize();
}
