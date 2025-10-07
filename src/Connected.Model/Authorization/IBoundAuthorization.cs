namespace Connected.Authorization;
public interface IBoundAuthorization : IAuthorization
{
	string Entity { get; }
	string EntityId { get; }
}
