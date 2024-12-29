using System.Data;

namespace Connected.Storage.Sql;

internal sealed class SqlStorageParameter : IStorageParameter
{
	public string? Name { get; init; }
	public object? Value { get; set; }
	public ParameterDirection Direction { get; init; } = ParameterDirection.Input;
	public DbType Type { get; init; } = DbType.String;
}