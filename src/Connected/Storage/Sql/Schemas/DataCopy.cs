using Connected.Storage.Schemas;
using System.Data;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class DataCopy(ExistingSchema existing, string temporaryName) : TableTransaction
{
	private ExistingSchema Existing { get; } = existing;
	public string TemporaryName { get; } = temporaryName;

	protected override async Task OnExecute()
	{
		await Context.Execute(CommandText);
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();
			var columnSet = new StringBuilder();
			var sourceSet = new StringBuilder();
			var comma = string.Empty;

			foreach (var column in Context.Schema.Columns)
			{
				if (column.IsVersion)
					continue;

				var existing = Existing.Columns.FirstOrDefault(f => string.Equals(column.Name, f.Name, StringComparison.OrdinalIgnoreCase));

				if (existing is null)
					continue;

				columnSet.Append($"{comma}{Escape(column.Name)}");

				if (NeedsConversion(column) && (existing.DataType != column.DataType || existing.Precision != column.Precision || existing.Scale != column.Scale))
					sourceSet.Append($"{comma}CONVERT({ConversionString(column)},{Escape(column.Name)})");
				else
					sourceSet.Append($"{comma}{Escape(column.Name)}");

				comma = ",";
			}

			text.AppendLine($"IF EXISTS (SELECT * FROM {Escape(Existing.Schema, Existing.Name)})");
			text.AppendLine($"INSERT INTO {Escape(Context.Schema.Schema, TemporaryName)} ({columnSet.ToString()})");
			text.AppendLine($"SELECT {sourceSet.ToString()} FROM {Escape(Existing.Schema, Existing.Name)}");

			return text.ToString();
		}
	}

	private static string ConversionString(ISchemaColumn column)
	{
		return column.DataType switch
		{
			DbType.Byte => "tinyint",
			DbType.Currency => "money",
			DbType.Decimal => $"decimal({column.Precision}, {column.Scale})",
			DbType.Double => "real",
			DbType.Int16 => "smallint",
			DbType.Int32 => "int",
			DbType.Int64 => "bigint",
			DbType.SByte => "smallint",
			DbType.Single => "float",
			DbType.UInt16 => "int",
			DbType.UInt32 => "bigint",
			DbType.UInt64 => "float",
			DbType.VarNumeric => $"numeric({column.Precision}, {column.Scale})",
			_ => throw new NotSupportedException(),
		};
	}

	private static bool NeedsConversion(ISchemaColumn column)
	{
		return column.DataType switch
		{
			DbType.Byte or DbType.Currency or DbType.Decimal or DbType.Double or DbType.Int16 or DbType.Int32 or DbType.Int64 or DbType.SByte
			or DbType.Single or DbType.UInt16 or DbType.UInt32 or DbType.UInt64 or DbType.VarNumeric => true,
			_ => false,
		};
	}
}
