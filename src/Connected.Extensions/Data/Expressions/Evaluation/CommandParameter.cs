using Connected.Data.Expressions.Languages;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class CommandParameter(string name, Type type, DataType dataType)
{
	public string Name { get; } = name;
	public Type Type { get; } = type;
	public DataType DataType { get; } = dataType;
}
