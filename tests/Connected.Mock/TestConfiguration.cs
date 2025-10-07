using Microsoft.Extensions.Configuration;

namespace Connected.Core.Mock;
public static class TestConfiguration
{
	public static string Url { get; private set; } = "http://localhost:5000";
	public static string Authentication = "NDVFRTlFN0UtNUJBRi00N0Q1LThGRjMtRTRDODQ1QTUyQjM1";
	public static void Initialize(IConfiguration configuration)
	{
		var section = configuration.GetSection("url");

		if (!string.IsNullOrWhiteSpace(section.Value))
			Url = section.Value;
	}

	public static string ParseUrl(string serviceUrl, string operation)
	{
		return $"{Url}/{serviceUrl}/{operation}";
	}
}
