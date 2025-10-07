namespace Connected.Configuration;

public interface IStorageConfiguration
{
	IDatabaseConfiguration Databases { get; }
}
