using System.Collections.ObjectModel;
using System.Collections.Generic;
using Connected.Data.Expressions.Collections;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class Command
{
	public Command(string commandText, IEnumerable<CommandParameter> parameters)
	{
		CommandText = commandText;
		Parameters = parameters.ToReadOnly();
	}

	public string CommandText { get; }
	public ReadOnlyCollection<CommandParameter> Parameters { get; }
}
