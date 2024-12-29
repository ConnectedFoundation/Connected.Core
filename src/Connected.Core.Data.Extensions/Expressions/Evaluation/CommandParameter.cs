using Connected.Data.Expressions.Languages;
using System;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class CommandParameter
{
	public CommandParameter(string name, Type type, DataType dataType)
	{
		Name = name;
		Type = type;
		DataType = dataType;
	}

	public string Name { get; }
	public Type Type { get; }
	public DataType DataType { get; }
}
