namespace Connected.Configuration.Authentication;

public interface IJwTokenConfiguration
{
	string? Issuer { get; }
	string? Audience { get; }
	string? Key { get; }
	int Duration { get; }
}