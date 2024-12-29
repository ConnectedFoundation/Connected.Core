namespace Connected.Storage.Sql.Schemas;

internal abstract class SynchronizationCommand
{
	public static string Escape(string value)
	{
		return $"[{Unescape(value)}]";
	}

	public static string Unescape(string value)
	{
		return value.TrimStart('[').TrimEnd(']');
	}

	public static string Escape(string schema, string name)
	{
		return $"{Escape(schema)}.{Escape(name)}";
	}
}
