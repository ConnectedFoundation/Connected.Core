namespace Connected.Services;

/// <summary>
/// Represents the base interface for all services.
/// </summary>
/// <remarks>
/// This interface serves as a marker for service implementations and extends IDisposable
/// to ensure proper resource cleanup. All service implementations should directly or
/// indirectly implement this interface.
/// </remarks>
public interface IService
	: IDisposable
{
}
