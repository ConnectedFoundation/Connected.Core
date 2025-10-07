using Connected.Storage;
using System.Data;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class StorageParameter : IStorageParameter
{
	public string? Name { get; init; }
	public object? Value { get; set; }
	public ParameterDirection Direction { get; init; }
	public DbType Type { get; init; }
}