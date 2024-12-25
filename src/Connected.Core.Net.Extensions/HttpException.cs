using System.Net;

namespace Connected.Net;

public class HttpException : Exception
{
	private string? _message;

	public HttpException(HttpStatusCode code) : this(code, null)
	{

	}

	public HttpException(HttpStatusCode code, string? message) : this(code, null, message)
	{
	}

	public HttpException(HttpStatusCode code, string? source, string? message)
	{
		Code = code;
		Source = source;

		_message = message;
	}

	public HttpStatusCode Code { get; }

	public override string Message => _message ?? Code.ToString();
}