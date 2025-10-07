using System.Data;

namespace Connected.Storage.Sql;

internal class SqlStorageOperation : IStorageOperation
{
	public SqlStorageOperation()
	{
		Parameters = [];
		Variables = [];
	}

	public List<IStorageParameter> Parameters { get; }
	public List<IStorageVariable> Variables { get; }
	public string? CommandText { get; set; }
	public CommandType CommandType { get; set; } = CommandType.Text;
	public DataConcurrencyMode Concurrency { get; set; } = DataConcurrencyMode.Disabled;
	public int CommandTimeout { get; set; }
}