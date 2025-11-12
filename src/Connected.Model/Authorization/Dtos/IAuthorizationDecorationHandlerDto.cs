using Connected.Services;

namespace Connected.Authorization.Dtos;
/// <summary>
/// DTO used by authorization decoration handlers to pass the resolved <see cref="IAuthorization"/>
/// middleware instance through the pipeline.
/// </summary>
/// <remarks>
/// Implementations supply the concrete authorization middleware that should process the decorated
/// component in the current context.
/// </remarks>
public interface IAuthorizationDecorationHandlerDto
	: IDto
{
	/// <summary>
	/// Gets or sets the authorization middleware instance applied by the handler.
	/// </summary>
	/// <remarks>
	/// Holds the authorization middleware instance that will be executed by the decoration handler
	/// for the current request or operation.
	/// </remarks>
	IAuthorization Middleware { get; set; }
}
