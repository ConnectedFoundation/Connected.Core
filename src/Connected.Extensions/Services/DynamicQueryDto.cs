using Connected.Annotations;
using System.Text.Json.Serialization;

namespace Connected.Services;

public abstract class DynamicQueryDto<TEntity>
	: QueryDto, IDynamicQueryDto<TEntity>
{
	[SkipValidation, JsonIgnore, Mergeable(false)]
	public Func<TEntity, bool>? Predicate { get; set; }
}
