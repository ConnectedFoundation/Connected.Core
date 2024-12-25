namespace Connected.Web;

public static class WebUtils
{
	public static string RootUrl
	{
		get
		{
			var request = Core.HttpContext?.Request;

			if (request is null)
				throw new NullReferenceException("No active Http request.");

			var forwardedScheme = request.Headers["X-Forwarded-Proto"].ToString();
			var scheme = request.Scheme;

			if (!string.IsNullOrWhiteSpace(forwardedScheme))
				scheme = forwardedScheme;

			return $"{scheme}://{request.Host}/{request.PathBase.ToString().Trim('/')}".TrimEnd('/');
		}
	}
}