using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace Connected.Net.Http;

public static class MultipartRequestHelper
{
	/*
    * Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
	 * The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
    */
	public static string GetBoundary(this MediaTypeHeaderValue contentType, int lengthLimit)
	{
		var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

		if (string.IsNullOrWhiteSpace(boundary))
			throw new InvalidDataException(HttpStrings.ErrMissingContentTypeBoundary);

		if (boundary.Length > lengthLimit)
			throw new InvalidDataException($"{HttpStrings.ErrMultipartBoundaryLengthLimit} ('{lengthLimit}')");

		return boundary;
	}

	public static bool IsMultipartContentType(string contentType)
	{
		return !string.IsNullOrEmpty(contentType) && contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase);
	}

	public static bool HasFormDataContentDisposition(this ContentDispositionHeaderValue contentDisposition)
	{
		/*
       * Content-Disposition: form-data; name="key";
       */
		return contentDisposition is not null
			 && contentDisposition.DispositionType.Equals("form-data")
			 && string.IsNullOrEmpty(contentDisposition.FileName.Value)
			 && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
	}

	public static bool HasFileContentDisposition(this ContentDispositionHeaderValue contentDisposition)
	{
		/*
       * Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
       */
		return contentDisposition is not null
			 && contentDisposition.DispositionType.Equals("form-data")
			 && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
				  || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
	}

	public static Encoding ResolveEncoding(this MultipartSection section)
	{
		var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);

		if (!hasMediaTypeHeader || mediaType is null)
			return Encoding.UTF8;

		return mediaType.Encoding ?? Encoding.UTF8;
	}
}
