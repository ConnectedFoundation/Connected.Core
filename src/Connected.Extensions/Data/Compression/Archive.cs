using System.IO.Compression;

namespace Connected.Data.Compression;

public static class Archive
{
	public static byte[] Create(List<NamedBuffer> files)
	{
		if (files.Any(e => string.IsNullOrWhiteSpace(e.Name)))
			throw new ArgumentException("One or more files have an invalid name");

		using var ms = new MemoryStream();
		using var archive = new ZipArchive(ms, ZipArchiveMode.Create);

		foreach (var file in files)
		{
			if (file.Name is null)
				throw new NullReferenceException("File name is null");

			var entry = archive.CreateEntry(file.Name);
			using var entryStream = entry.Open();

			entryStream.Write(file.Content);
		}

		return ms.ToArray();
	}
}