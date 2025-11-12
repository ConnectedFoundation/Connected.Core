using Connected.Annotations.Entities;
using Connected.Storage.Schemas;
using System.Data;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class Columns(ExistingSchema existing) : SynchronizationQuery<List<ISchemaColumn>>
{
	protected override async Task<List<ISchemaColumn>> OnExecute()
	{
		var result = new List<ISchemaColumn>();
		var rdr = await Context.OpenReader(new SqlStorageOperation { CommandText = CommandText });

		if (rdr is null)
			return result;

		while (rdr.Read())
		{
			var column = new ExistingColumn(existing)
			{
				IsNullable = !string.Equals(rdr.GetValue("IS_NULLABLE", string.Empty), "NO", StringComparison.OrdinalIgnoreCase),
				DataType = SchemaExtensions.ToDbType(rdr.GetValue("DATA_TYPE", string.Empty)),
				MaxLength = rdr.GetValue("CHARACTER_MAXIMUM_LENGTH", 0),
				Name = rdr.GetValue("COLUMN_NAME", string.Empty),
			};

			if (column.DataType == DbType.Decimal || column.DataType == DbType.VarNumeric)
			{
				column.Precision = rdr.GetValue("NUMERIC_PRECISION", 0);
				column.Scale = rdr.GetValue("NUMERIC_SCALE", 0);
			}

			if (column.DataType == DbType.DateTime2
				 || column.DataType == DbType.Time
				 || column.DataType == DbType.DateTimeOffset)
				column.DatePrecision = rdr.GetValue("DATETIME_PRECISION", 0);

			if (column.DataType == DbType.Date)
				column.DateKind = DateKind.Date;
			else if (column.DataType == DbType.DateTime)
			{
				if (string.Compare(rdr.GetValue("DATA_TYPE", string.Empty), "smalldatetime", true) == 0)
					column.DateKind = DateKind.SmallDateTime;
			}
			else if (column.DataType == DbType.DateTime2)
				column.DateKind = DateKind.DateTime2;
			else if (column.DataType == DbType.Time)
				column.DateKind = DateKind.Time;
			else if (column.DataType == DbType.Binary)
			{
				if (string.Compare(rdr.GetValue("DATA_TYPE", string.Empty), "varbinary", true) == 0)
					column.BinaryKind = BinaryKind.VarBinary;
				else if (string.Compare(rdr.GetValue("DATA_TYPE", string.Empty), "binary", true) == 0)
					column.BinaryKind = BinaryKind.Binary;
			}

			column.IsVersion = string.Equals(rdr.GetValue("DATA_TYPE", string.Empty), "timestamp", StringComparison.OrdinalIgnoreCase);

			result.Add(column);
		}

		rdr.Close();

		return result;
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{Context.Schema.Schema}' AND TABLE_NAME = '{Context.Schema.Name}'");

			return text.ToString();
		}
	}
}
