using System.Collections.Immutable;

namespace Connected.Storage.Schemas;

/// <summary>
/// Provides operations for querying existing schema column information.
/// </summary>
/// <remarks>
/// This interface enables inspection of existing database schema columns, particularly
/// for retrieving index information associated with specific columns. This is useful during
/// schema synchronization and migration operations where the application needs to understand
/// the current state of the database schema before making changes. The interface provides
/// read-only access to schema metadata without modifying the underlying database structure.
/// </remarks>
public interface IExistingSchemaColumn
{
	/// <summary>
	/// Queries the names of indexes that include the specified column.
	/// </summary>
	/// <param name="column">The column name to search for in indexes.</param>
	/// <returns>
	/// An immutable array containing the names of all indexes that include the specified column.
	/// </returns>
	/// <remarks>
	/// This method searches through the database schema to find all indexes that contain
	/// the specified column as part of their definition. This information is useful for
	/// understanding column usage in indexing strategies and for making informed decisions
	/// during schema modifications. The immutable array ensures that the returned collection
	/// cannot be modified after retrieval.
	/// </remarks>
	ImmutableArray<string> QueryIndexColumns(string column);
}
