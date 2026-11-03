using Connected.Storage.PostgreSql;

namespace Connected.Storage.Tests;

internal sealed class TestConnectionDto : IStorageConnectionDto
{
	public string? ConnectionString { get; set; }
	public StorageConnectionMode Mode { get; set; } = StorageConnectionMode.Shared;
}

/// <summary>A command with no parameters, for driving a storage connection directly.</summary>
internal sealed class TextCommand(IStorageConnection connection, string text) : IStorageCommand
{
	public IStorageOperation Operation { get; } = new PostgreSqlStorageOperation { CommandText = text };
	public IStorageConnection Connection { get; } = connection;

	public void Dispose()
	{
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
