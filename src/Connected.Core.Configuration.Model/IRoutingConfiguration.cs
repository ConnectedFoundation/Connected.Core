namespace Connected.Configuration;

public interface IRoutingConfiguration
{
	string? BaseUrl { get; }
	string? RoutingServerUrl { get; }
}