using Connected.Data.Expressions.Collections;
using System.Collections.ObjectModel;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class Command(string commandText, IEnumerable<CommandParameter> parameters)
{
	public string CommandText { get; } = commandText;
	public ReadOnlyCollection<CommandParameter> Parameters { get; } = parameters.ToReadOnly();
}
