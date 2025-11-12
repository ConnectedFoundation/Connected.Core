namespace Connected.Storage.Sql.Schemas;

public enum ConstraintType
{
	Unknown = 0,
	Default = 1,
	Unique = 2,
	PrimaryKey = 3
}
internal class ObjectConstraint
{
	public string? Type { get; set; }
	public string? Name { get; set; }
	public string? DeleteAction { get; set; }
	public string? UpdateAction { get; set; }
	public string? StatusEnabled { get; set; }
	public string? StatusForReplication { get; set; }
	public string? Keys { get; set; }

	public ConstraintType ConstraintType
	{
		get
		{
			if (Type is null)
				return ConstraintType.Unknown;

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

	public List<string> Columns
	{
		get
		{
			var result = new List<string>();

			switch (ConstraintType)
			{
				case ConstraintType.Default:
					if (Type is not null)
						result.Add(Type.Split(' ')[^1].Trim());

					break;
				case ConstraintType.Unique:
				case ConstraintType.PrimaryKey:
					var tokens = Keys is null ? [] : Keys.Split(',');

					foreach (var token in tokens)
						result.Add(token);
					break;
			}

			return result;
		}
	}

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
