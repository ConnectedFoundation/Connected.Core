using System.Linq.Expressions;

namespace Connected.Services;

public interface IDynamicQueryDto<TEntity>
	: IQueryDto
{
	Expression<Func<TEntity, bool>>? Predicate { get; set; }
}
