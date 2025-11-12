using System.Data;

namespace Connected.Storage.Sql.Schemas;

internal class SpHelp
	: SynchronizationQuery<ObjectDescriptor>
{
	private readonly ObjectDescriptor _descriptor;

	public SpHelp()
	{
		_descriptor = new();
	}

	private ObjectDescriptor Result => _descriptor;

	protected override async Task<ObjectDescriptor> OnExecute()
	{
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

	private void ReadMetadata(IDataReader rdr)
	{
		if (rdr.Read())
		{
			Result.MetaData.Name = rdr.GetValue("Name", string.Empty);
			Result.MetaData.Owner = rdr.GetValue("Owner", string.Empty);
			Result.MetaData.Type = rdr.GetValue(FieldNameProvider.Resolve(Context.Version, FieldNameKind.Type), string.Empty);
		}
	}

	private void ReadColumns(IDataReader rdr)
	{
		rdr.NextResult();

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

	private void ReadRowGuid(IDataReader rdr)
	{
		rdr.NextResult();

		if (rdr.Read())
			Result.RowGuid.RowGuidCol = rdr.GetValue("RowGuidCol", string.Empty);
	}

	private void ReadFileGroup(IDataReader rdr)
	{
		rdr.NextResult();

		if (rdr.Read())
			Result.FileGroup.FileGroup = rdr.GetValue("Data_located_on_filegroup", string.Empty);
	}

	private void ReadIndexes(IDataReader rdr)
	{
		rdr.NextResult();

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

	private void ReadConstraints(IDataReader rdr)
	{
		rdr.NextResult();

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
