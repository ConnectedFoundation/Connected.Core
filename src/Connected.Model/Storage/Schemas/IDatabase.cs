namespace Connected.Storage.Schemas;

public interface IDatabase
{
	List<ITable> Tables { get; }
}
