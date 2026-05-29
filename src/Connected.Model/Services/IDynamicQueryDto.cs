namespace Connected.Services;

public interface IDynamicQueryDto<TEntity>
	: IQueryDto
{
	Func<TEntity, bool>? Predicate { get; set; }
}
