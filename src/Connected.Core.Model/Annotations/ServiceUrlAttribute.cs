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
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Event)]
public sealed class ServiceUrlAttribute : Attribute
{
	/// <summary>
	/// Creates a new instance of the ServiceUrlAttribute class.
	/// </summary>
	/// <param name="url">An url to be used by endpoint mappings to serve the request to external clients.</param>
	public ServiceUrlAttribute(string url)
	{
		Url = url;
	}
	/// <summary>
	/// Gets the url that should be used for accessible Service or Service operation.
	/// </summary>
	public string Url { get; }
}
