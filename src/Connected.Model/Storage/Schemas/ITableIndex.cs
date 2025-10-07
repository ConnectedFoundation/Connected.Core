namespace Connected.Storage.Schemas;

public interface ITableIndex
{
	string Name { get; }
	List<string> Columns { get; }
}
