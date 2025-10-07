using Connected.Storage;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class StorageVariable : IStorageVariable
{
	public StorageVariable()
	{
		Values = new();
	}

	public required string Name { get; init; }
	public List<object?> Values { get; init; }
}