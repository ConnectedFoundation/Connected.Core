using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Connected.Net.Http;

public static class FileHelpers
{
	public static async Task<byte[]> ProcessStreamedFile(MultipartSection section, ContentDispositionHeaderValue contentDisposition,
		 string[] permittedExtensions, long sizeLimit)
	{
		using var memoryStream = new MemoryStream();

		await section.Body.CopyToAsync(memoryStream);
		/*
		 * Check if the file is empty or exceeds the size limit.
		 */
		if (memoryStream.Length == 0)
			throw new InvalidDataException(HttpStrings.ErrFileEmpty);
		else if (memoryStream.Length > sizeLimit)
		{
			var megabyteSizeLimit = sizeLimit / 1048576;

			throw new InvalidDataException($"{HttpStrings.ErrFileToBig}  {megabyteSizeLimit:N1} MB.");
		}
		else if (!Validate(contentDisposition.FileName.Value, memoryStream, permittedExtensions))
			throw new InvalidDataException(HttpStrings.ErrFileNotAllowed);
		else
			return memoryStream.ToArray();
	}

	private static bool Validate(string? fileName, Stream data, string[] permittedExtensions)
	{
		if (string.IsNullOrEmpty(fileName) || data is null || data.Length == 0)
			return false;

		var ext = Path.GetExtension(fileName).ToLowerInvariant();

		if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
			return false;

		return true;
	}
}
