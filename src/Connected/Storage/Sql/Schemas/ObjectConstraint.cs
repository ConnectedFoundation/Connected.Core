namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Specifies the type of database constraint.
/// </summary>
public enum ConstraintType
{
	/// <summary>
	/// Unknown or unrecognized constraint type.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Default value constraint.
	/// </summary>
	Default = 1,

	/// <summary>
	/// Unique constraint ensuring column values are distinct.
	/// </summary>
	Unique = 2,

	/// <summary>
	/// Primary key constraint uniquely identifying rows.
	/// </summary>
	PrimaryKey = 3
}

/// <summary>
/// Represents constraint metadata retrieved from SQL Server system catalogs.
/// </summary>
/// <remarks>
/// This class encapsulates constraint information obtained from the sp_help stored procedure or
/// similar system catalog queries. It provides metadata about constraint properties including
/// type, name, associated columns, and referential actions. The class intelligently parses the
/// raw constraint type string to determine the specific constraint category and extracts column
/// information from the constraint definition. For default constraints, it also provides access
/// to the default value expression. This information is essential for schema comparison and
/// synchronization operations that need to understand existing constraint definitions.
/// </remarks>
internal class ObjectConstraint
{
	/// <summary>
	/// Gets or sets the constraint type string as returned by system catalogs.
	/// </summary>
	/// <value>
	/// The raw constraint type description (e.g., "DEFAULT on column...", "PRIMARY KEY clustered").
	/// </value>
	public string? Type { get; set; }

	/// <summary>
	/// Gets or sets the constraint name.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the delete action for foreign key constraints.
	/// </summary>
	/// <value>
	/// The referential action on delete (e.g., "CASCADE", "NO ACTION"), or <c>null</c> if not applicable.
	/// </value>
	public string? DeleteAction { get; set; }

	/// <summary>
	/// Gets or sets the update action for foreign key constraints.
	/// </summary>
	/// <value>
	/// The referential action on update (e.g., "CASCADE", "NO ACTION"), or <c>null</c> if not applicable.
	/// </value>
	public string? UpdateAction { get; set; }

	/// <summary>
	/// Gets or sets the enabled status of the constraint.
	/// </summary>
	/// <value>
	/// A string indicating whether the constraint is enabled, or <c>null</c> if not applicable.
	/// </value>
	public string? StatusEnabled { get; set; }

	/// <summary>
	/// Gets or sets the replication status of the constraint.
	/// </summary>
	/// <value>
	/// A string indicating the constraint's replication behavior, or <c>null</c> if not applicable.
	/// </value>
	public string? StatusForReplication { get; set; }

	/// <summary>
	/// Gets or sets the keys or column list associated with the constraint.
	/// </summary>
	/// <value>
	/// A comma-separated list of column names or default value expression.
	/// </value>
	public string? Keys { get; set; }

	/// <summary>
	/// Gets the parsed constraint type.
	/// </summary>
	/// <value>
	/// The constraint type category determined from the Type string.
	/// </value>
	/// <remarks>
	/// This property parses the raw Type string to identify whether the constraint is a
	/// default, unique, or primary key constraint based on string prefixes.
	/// </remarks>
	public ConstraintType ConstraintType
	{
		get
		{
			if (Type is null)
				return ConstraintType.Unknown;

			/*
			 * Determine constraint type from the type string prefix.
			 */
			if (Type.StartsWith("DEFAULT "))
				return ConstraintType.Default;
			else if (Type.StartsWith("UNIQUE "))
				return ConstraintType.Unique;
			else if (Type.StartsWith("PRIMARY KEY "))
				return ConstraintType.PrimaryKey;
			else
				return ConstraintType.Unknown;
		}
	}

	/// <summary>
	/// Gets the list of columns involved in the constraint.
	/// </summary>
	/// <value>
	/// A list of column names participating in the constraint.
	/// </value>
	/// <remarks>
	/// For default constraints, extracts the column name from the Type string.
	/// For unique and primary key constraints, parses the Keys string to get all columns.
	/// </remarks>
	public List<string> Columns
	{
		get
		{
			var result = new List<string>();

			/*
			 * Extract column information based on constraint type.
			 */
			switch (ConstraintType)
			{
				case ConstraintType.Default:
					/*
					 * For default constraints, the column name is at the end of the Type string.
					 */
					if (Type is not null)
						result.Add(Type.Split(' ')[^1].Trim());

					break;

				case ConstraintType.Unique:
				case ConstraintType.PrimaryKey:
					/*
					 * For unique and primary key constraints, parse the comma-separated Keys list.
					 */
					var tokens = Keys is null ? [] : Keys.Split(',');

					foreach (var token in tokens)
						result.Add(token);
					break;
			}

			return result;
		}
	}

	/// <summary>
	/// Gets the default value expression for default constraints.
	/// </summary>
	/// <value>
	/// The default value expression without parentheses, or the raw Keys value if not a standard format.
	/// </value>
	/// <remarks>
	/// Default value expressions are typically wrapped in parentheses. This property removes
	/// the wrapping parentheses to get the actual expression.
	/// </remarks>
	public string? DefaultValue
	{
		get
		{
			if (ConstraintType == ConstraintType.Default && Keys is not null && Keys.StartsWith('(') && Keys.EndsWith(')'))
				return Keys[1..^1];

			return Keys;
		}
	}
}
