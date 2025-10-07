namespace Connected.Services;
public interface IDistributedPrimaryKeyListDto<THead, TPrimaryKey> : IDto
	where THead : notnull
	where TPrimaryKey : notnull
{
	List<Tuple<THead, TPrimaryKey>> Items { get; set; }
}
