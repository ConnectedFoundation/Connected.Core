using Connected.Storage;
using System.Data;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class StorageOperation
	: IStorageOperation
{
	public StorageOperation()
	{
		Parameters = [];
		Variables = [];
		BoundProperties = [];
	}

	public string? CommandText { get; set; }
	public CommandType CommandType { get; set; } = CommandType.Text;
	public List<IStorageParameter> Parameters { get; }
	public List<IStorageVariable> Variables { get; }
	public Dictionary<string, object?> BoundProperties { get; }
	public int CommandTimeout { get; set; }
	public DataConcurrencyMode Concurrency { get; set; }
}