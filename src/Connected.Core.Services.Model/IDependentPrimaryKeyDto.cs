namespace Connected.Services;

public interface IDependentPrimaryKeyDto<THead, TPrimaryKey> : IDto
	where THead : notnull
	where TPrimaryKey : notnull
{

	THead Head { get; set; }
	TPrimaryKey Id { get; set; }
}
