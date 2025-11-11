using Connected.Services;

namespace Connected.Storage.Sql;

internal sealed class SqlDataConnection(ICancellationContext context)
	: SqlStorageConnection(context)
{
}
