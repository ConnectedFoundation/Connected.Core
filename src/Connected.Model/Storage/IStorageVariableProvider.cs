using System.Collections.Immutable;

namespace Connected.Storage;

public interface IStorageVariableProvider
{
	IImmutableList<IStorageVariable> Variables { get; }

	void AddVariable(string name, object? value);
	void RemoveVariable(string name);
}
