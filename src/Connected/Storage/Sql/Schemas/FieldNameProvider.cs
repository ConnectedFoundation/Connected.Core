namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Specifies the type of field name to resolve.
/// </summary>
internal enum FieldNameKind
{
	/// <summary>
	/// Represents the type field name.
	/// </summary>
	Type = 0
}

/// <summary>
/// Specifies the SQL Server version.
/// </summary>
internal enum SqlVersion
{
	/// <summary>
	/// Default SQL Server version (pre-2022).
	/// </summary>
	Default = 0,

	/// <summary>
	/// SQL Server 2022 (version 16) or later.
	/// </summary>
	Sixteen = 1
}

/// <summary>
/// Provides field name resolution based on SQL Server version.
/// </summary>
/// <remarks>
/// This static class resolves database field names that vary between SQL Server versions.
/// For example, some system catalog views and functions use different column names in
/// SQL Server 2022 (version 16) compared to earlier versions. The provider maps version-specific
/// field names to a common enumeration, ensuring compatibility across different SQL Server versions.
/// This is particularly important when querying system metadata where column names have changed
/// between releases.
/// </remarks>
internal static class FieldNameProvider
{
	/// <summary>
	/// Resolves a field name based on the SQL Server version and field kind.
	/// </summary>
	/// <param name="version">The SQL Server version.</param>
	/// <param name="kind">The type of field name to resolve.</param>
	/// <returns>The version-appropriate field name.</returns>
	/// <exception cref="NotSupportedException">Thrown when the field kind is not supported.</exception>
	public static string Resolve(Version version, FieldNameKind kind)
	{
		return kind switch
		{
			FieldNameKind.Type => ResolveKind(version),
			_ => throw new NotSupportedException()
		};
	}

	/// <summary>
	/// Resolves the type field name for the specified SQL Server version.
	/// </summary>
	/// <param name="version">The SQL Server version.</param>
	/// <returns>The appropriate field name for the type column.</returns>
	/// <exception cref="NotSupportedException">Thrown when the SQL version is not supported.</exception>
	/// <remarks>
	/// SQL Server 2022 changed the "Object_type" column name to "Type" in certain system views.
	/// This method ensures the correct column name is used based on the server version.
	/// </remarks>
	private static string ResolveKind(Version version)
	{
		return ResolveVersion(version) switch
		{
			SqlVersion.Default => "Object_type",
			SqlVersion.Sixteen => "Type",
			_ => throw new NotSupportedException()
		};
	}

	/// <summary>
	/// Determines the SQL Server version category.
	/// </summary>
	/// <param name="version">The SQL Server version.</param>
	/// <returns>The SQL version category.</returns>
	/// <remarks>
	/// SQL Server version 16 corresponds to SQL Server 2022. Earlier versions use
	/// different system catalog structures and naming conventions.
	/// </remarks>
	private static SqlVersion ResolveVersion(Version version)
	{
		if (version.Major >= 16)
			return SqlVersion.Sixteen;

		return SqlVersion.Default;
	}
}