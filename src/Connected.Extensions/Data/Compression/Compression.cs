using Connected.Reflection;
using System.IO.Compression;
using System.Text;

namespace Connected.Data.Compression;

public static class Compression
{
	public static async Task<byte[]> Compress(object? value)
	{
		if (value is null)
			return [];

		var serialized = await Serializer.Serialize(value);

		if (serialized is null)
			return [];

		var buffer = value is byte[] v ? v : Encoding.UTF8.GetBytes(serialized);

		using var output = new MemoryStream();
		using var zip = new GZipStream(output, CompressionMode.Compress);
		using var input = new MemoryStream(buffer);

		await input.CopyToAsync(zip);

		zip.Flush();

		return output.ToArray();
	}

	public static async Task<byte[]> Decompress(byte[]? compressed)
	{
		if (compressed is null)
			return [];

		using var input = new MemoryStream(compressed);
		using var zip = new GZipStream(input, CompressionMode.Decompress);
		using var output = new MemoryStream();

		await zip.CopyToAsync(output);

		return output.ToArray();
	}
}