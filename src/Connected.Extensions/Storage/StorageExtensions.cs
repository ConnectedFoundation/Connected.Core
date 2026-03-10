namespace Connected.Storage;

public static class StorageExtensions
{
	public static IStorageOperation WithStorageVariables(this IStorageOperation operation, IStorageVariableProvider? variables)
	{
		if (variables is null)
			return operation;

		foreach (var variable in variables.Variables)
		{
			if (operation.Variables.FirstOrDefault(f => string.Equals(f.Name, variable.Name, StringComparison.OrdinalIgnoreCase)) is IStorageVariable existing)
				continue;

			operation.Variables.Add(variable);
		}

		return operation;
	}
}
