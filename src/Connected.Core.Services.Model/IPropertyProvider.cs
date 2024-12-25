using System.Collections.Generic;

namespace Connected.Services;

public interface IPropertyProvider
{
	Dictionary<string, object?> Properties { get; }
}
