namespace Connected.Authorization.Services;
public abstract class BoundServiceAuthorization : ServiceAuthorization, IBoundAuthorization
{
	public abstract string Entity { get; }

	public abstract string EntityId { get; }

}
