using Connected.Annotations;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Connected.Services;

public abstract class DynamicQueryDto<TEntity>
	: QueryDto, IDynamicQueryDto<TEntity>
{
	[SkipValidation, JsonIgnore, Mergeable(false)]
	public Expression<Func<TEntity, bool>>? Predicate { get; set; }
}
