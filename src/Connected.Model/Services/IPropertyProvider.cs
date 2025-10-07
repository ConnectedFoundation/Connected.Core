namespace Connected.Services;

public interface IPropertyProvider
{
	Dictionary<string, object?> Properties { get; set; }
}
