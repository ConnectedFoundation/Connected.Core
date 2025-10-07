namespace Connected.Identities;
public interface IIdentityAccessor
{
	IIdentity? Identity { get; }
}
