namespace Connected.Net.Rest.OpenApi.Configuration;

/// <summary>
/// Overrides for values the OpenAPI document generator would otherwise compute at runtime.
/// Both are optional: when unset, the generator falls back to its existing dynamic behavior
/// (<see cref="Connected.Configuration.IRoutingConfiguration.BaseUrl"/> for the server url,
/// <c>identities:maintenance</c> for the bearer token Scalar prefills). Setting a key here
/// fixes that value regardless of the dynamic source.
/// </summary>
public sealed class OpenApiOptions
{
	public const string Path = "openApi";

	public string? BearerToken { get; set; }
	public string? ServerUrl { get; set; }
}
