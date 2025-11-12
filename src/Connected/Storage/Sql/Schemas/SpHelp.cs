using System.Data;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Executes the sp_help stored procedure to retrieve comprehensive table metadata.
/// </summary>
/// <remarks>
/// This query transaction executes SQL Server's sp_help system stored procedure to retrieve
/// detailed metadata about a table including columns, indexes, constraints, identity settings,
/// ROWGUIDCOL configuration, and filegroup placement. The stored procedure returns multiple
/// result sets which are parsed into an ObjectDescriptor containing organized metadata. This
/// comprehensive metadata is essential for schema comparison operations to determine what
/// changes are needed to synchronize entity definitions with existing database structures.
/// The class handles all result sets returned by sp_help including metadata, columns, identity,
/// rowguid, filegroup, indexes, and constraints.
/// </remarks>
internal class SpHelp
	: SynchronizationQuery<ObjectDescriptor>
{
	private readonly ObjectDescriptor _descriptor;

	/// <summary>
	/// Initializes a new instance of the <see cref="SpHelp"/> class.
	/// </summary>
	public SpHelp()
	{
		_descriptor = new();
	}

	/// <summary>
	/// Gets the object descriptor containing the parsed metadata.
	/// </summary>
	private ObjectDescriptor Result => _descriptor;

	/// <inheritdoc/>
	protected override async Task<ObjectDescriptor> OnExecute()
	{
		/*
		 * Configure the sp_help stored procedure call with the fully qualified table name.
		 */
		var operation = new SqlStorageOperation
		{
			CommandText = "sp_help",
			CommandType = CommandType.StoredProcedure
		};

		operation.Parameters.Add(new SqlStorageParameter
		{
			Name = "@objname",
			Type = DbType.String,
			Value = Escape(Context.Schema.Schema, Context.Schema.Name)
		});

		var rdr = await Context.OpenReader(operation);

		try
		{
			/*
			 * Parse all result sets returned by sp_help in their defined order.
			 */
			ReadMetadata(rdr);
			ReadColumns(rdr);
			ReadIdentity(rdr);
			ReadRowGuid(rdr);
			ReadFileGroup(rdr);
			ReadIndexes(rdr);
			ReadConstraints(rdr);
		}
		finally
		{
			rdr.Close();
		}

		return Result;
	}

	/// <summary>
	/// Reads general object metadata from the first result set.
	/// </summary>
	/// <param name="rdr">The data reader positioned at the metadata result set.</param>
	private void ReadMetadata(IDataReader rdr)
	{
		if (rdr.Read())
		{
			Result.MetaData.Name = rdr.GetValue("Name", string.Empty);
			Result.MetaData.Owner = rdr.GetValue("Owner", string.Empty);
			Result.MetaData.Type = rdr.GetValue(FieldNameProvider.Resolve(Context.Version, FieldNameKind.Type), string.Empty);
		}
	}

	/// <summary>
	/// Reads column definitions from the second result set.
	/// </summary>
	/// <param name="rdr">The data reader positioned at the columns result set.</param>
	private void ReadColumns(IDataReader rdr)
	{
		rdr.NextResult();

		/*
		 * Process each column row and create ObjectColumn instances.
		 */
		while (rdr.Read())
		{
			var precision = rdr.GetValue("Prec", string.Empty).Trim();
			var scale = rdr.GetValue("Scale", string.Empty).Trim();

			if (string.IsNullOrEmpty(precision))
				precision = null;

			if (string.IsNullOrEmpty(scale))
				scale = null;

			Result.Columns.Add(new ObjectColumn
			{
				Collation = rdr.GetValue("Collation", string.Empty),
				Computed = !string.Equals(rdr.GetValue("Computed", string.Empty), "no", StringComparison.OrdinalIgnoreCase),
				FixedLenInSource = rdr.GetValue("FixedLenNullInSource", string.Empty),
				Length = rdr.GetValue("Length", 0),
				Name = rdr.GetValue("Column_name", string.Empty),
				Nullable = !string.Equals(rdr.GetValue("Nullable", string.Empty), "no", StringComparison.OrdinalIgnoreCase),
				Precision = Convert.ToInt32(precision),
				Scale = Convert.ToInt32(scale),
				TrimTrailingBlanks = rdr.GetValue("TrimTrailingBlanks", string.Empty),
				Type = rdr.GetValue("Type", string.Empty)
			});
		}
	}

	/// <summary>
	/// Reads identity column configuration from the third result set.
	/// </summary>
	/// <param name="rdr">The data reader positioned at the identity result set.</param>
	private void ReadIdentity(IDataReader rdr)
	{
		rdr.NextResult();

		if (rdr.Read())
		{
			Result.Identity.Identity = rdr.GetValue("Identity", string.Empty);
			Result.Identity.Increment = rdr.GetValue("Increment", 0);
			Result.Identity.NotForReplication = rdr.GetValue("Not For Replication", 0) != 0;
		}
	}

	/// <summary>
	/// Reads ROWGUIDCOL information from the fourth result set.
	/// </summary>
	/// <param name="rdr">The data reader positioned at the rowguid result set.</param>
	private void ReadRowGuid(IDataReader rdr)
	{
		rdr.NextResult();

		if (rdr.Read())
			Result.RowGuid.RowGuidCol = rdr.GetValue("RowGuidCol", string.Empty);
	}

	/// <summary>
	/// Reads filegroup placement information from the fifth result set.
	/// </summary>
	/// <param name="rdr">The data reader positioned at the filegroup result set.</param>
	private void ReadFileGroup(IDataReader rdr)
	{
		rdr.NextResult();

		if (rdr.Read())
			Result.FileGroup.FileGroup = rdr.GetValue("Data_located_on_filegroup", string.Empty);
	}

	/// <summary>
	/// Reads index definitions from the sixth result set.
	/// </summary>
	/// <param name="rdr">The data reader positioned at the indexes result set.</param>
	private void ReadIndexes(IDataReader rdr)
	{
		rdr.NextResult();

		/*
		 * Process each index row and create ObjectIndex instances.
		 */
		while (rdr.Read())
		{
			Result.Indexes.Add(new ObjectIndex
			{
				Description = rdr.GetValue("index_description", string.Empty),
				Keys = rdr.GetValue("index_keys", string.Empty),
				Name = rdr.GetValue("index_name", string.Empty)
			});
		}
	}

	/// <summary>
	/// Reads constraint definitions from the seventh result set.
	/// </summary>
	/// <param name="rdr">The data reader positioned at the constraints result set.</param>
	private void ReadConstraints(IDataReader rdr)
	{
		rdr.NextResult();

		/*
		 * Process each constraint row and create ObjectConstraint instances.
		 */
		while (rdr.Read())
		{
			Result.Constraints.Add(new ObjectConstraint
			{
				DeleteAction = rdr.GetValue("delete_action", string.Empty),
				Keys = rdr.GetValue("constraint_keys", string.Empty),
				Name = rdr.GetValue("constraint_name", string.Empty),
				StatusEnabled = rdr.GetValue("status_enabled", string.Empty),
				StatusForReplication = rdr.GetValue("status_for_replication", string.Empty),
				Type = rdr.GetValue("constraint_type", string.Empty),
				UpdateAction = rdr.GetValue("update_action", string.Empty)
			});
		}
	}
}
