namespace Connected.Identities;

public interface ISingleSignOnIdentity
{
	string? AuthenticationToken { get; init; }
}