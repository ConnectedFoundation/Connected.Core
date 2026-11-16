using System.Data;

namespace Connected.Storage;

public interface IStorageParameter
{
	string? Name { get; init; }
	object? Value { get; set; }
	ParameterDirection Direction { get; init; }
	DbType Type { get; init; }

	/// <summary>
	/// The size of a variable-length value a provider has to be told for an output (a binary version, say), or 0 where the provider infers it.
	/// </summary>
	int Size => 0;
}
