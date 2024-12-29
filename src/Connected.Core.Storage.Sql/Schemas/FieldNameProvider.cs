namespace Connected.Storage.Sql.Schemas;

internal enum FieldNameKind
{
	Type = 0
}

internal enum SqlVersion
{
	Default = 0,
	Sixteen = 1
}

internal static class FieldNameProvider
{
	public static string Resolve(Version version, FieldNameKind kind)
	{
		return kind switch
		{
			FieldNameKind.Type => ResolveKind(version),
			_ => throw new NotSupportedException()
		};
	}

	private static string ResolveKind(Version version)
	{
		return ResolveVersion(version) switch
		{
			SqlVersion.Default => "Object_type",
			SqlVersion.Sixteen => "Type",
			_ => throw new NotSupportedException()
		};
	}

	private static SqlVersion ResolveVersion(Version version)
	{
		if (version.Major >= 16)
			return SqlVersion.Sixteen;

		return SqlVersion.Default;
	}
}