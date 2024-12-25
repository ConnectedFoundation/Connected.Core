using System.Data;

namespace Connected.Storage;

public interface IStorageParameter
{
	string? Name { get; init; }
	object? Value { get; set; }
	ParameterDirection Direction { get; init; }
	DbType Type { get; init; }
}
