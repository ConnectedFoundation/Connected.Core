using System.Collections.Immutable;

namespace Connected.Runtime;

public interface IRuntimeImage
{
	void Register();

	IImmutableList<string> Dependencies { get; }
}
