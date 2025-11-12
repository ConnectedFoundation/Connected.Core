namespace Connected.Storage;

public interface IStorageWriter : IStorageCommand
{
	Task<int> Execute();
}
