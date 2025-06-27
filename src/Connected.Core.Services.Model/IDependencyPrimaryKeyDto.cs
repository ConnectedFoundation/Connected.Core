namespace Connected.Services;

public interface IDependencyPrimaryKeyDto<THead, TPrimaryKey> : IDto
	where THead : notnull
	where TPrimaryKey : notnull
{

	THead Head { get; set; }
	TPrimaryKey Id { get; set; }
}
