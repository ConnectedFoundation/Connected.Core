using System.Data;

namespace Connected.Storage;

public enum DataConcurrencyMode
{
	Enabled = 1,
	Disabled = 2,
}

public interface IStorageOperation
{
	string? CommandText { get; set; }
	CommandType CommandType { get; set; }

	List<IStorageParameter> Parameters { get; }
	List<IStorageVariable> Variables { get; }

	int CommandTimeout { get; set; }
	DataConcurrencyMode Concurrency { get; set; }
}
