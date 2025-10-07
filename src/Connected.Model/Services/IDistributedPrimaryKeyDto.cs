namespace Connected.Services;

public interface IDistributedPrimaryKeyDto<THead, TPrimaryKey> : IPrimaryKeyDto<TPrimaryKey>
	where THead : notnull
	where TPrimaryKey : notnull
{

	THead Head { get; set; }
}
