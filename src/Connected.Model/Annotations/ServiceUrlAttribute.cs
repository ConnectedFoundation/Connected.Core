namespace Connected.Annotations;
/// <summary>
/// Specifies the url from which the service or service operation is accessible.
/// </summary>
/// <remarks>
/// Service must provide an url to be accessible to external clients where Service operations
/// are not required to do so. They are accessibly automatically if the Service is accessible
/// and they contain at least one valid verb. If the Service operation is decorated with this attribute,
/// it's default url is replaced with the one specified in the attribute.
/// </remarks>
/// <remarks>
/// Creates a new instance of the ServiceUrlAttribute class.
/// </remarks>
/// <param name="url">An url to be used by endpoint mappings to serve the request to external clients.</param>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Event)]
public sealed class ServiceUrlAttribute(string url)
	: Attribute
{
	public const string Lookup = "lookup";

	/// <summary>
	/// Gets the url that should be used for accessible Service or Service operation.
	/// </summary>
	public string Url { get; } = url;
}
