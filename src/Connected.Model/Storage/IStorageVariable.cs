namespace Connected.Storage;

public interface IStorageVariable
{
	string Name { get; init; }
	List<object?> Values { get; init; }
}