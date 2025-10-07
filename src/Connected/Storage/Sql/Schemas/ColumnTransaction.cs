using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

internal abstract class ColumnTransaction(ISchemaColumn column) : TableTransaction
{
	protected ISchemaColumn Column { get; } = column;
}