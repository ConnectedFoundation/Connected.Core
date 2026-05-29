using Connected.Annotations;
using System.Text.Json.Serialization;

namespace Connected.Services;

public class DynamicQueryDto<TEntity>
	: QueryDto, IDynamicQueryDto<TEntity>
{
	[SkipValidation, JsonIgnore]
	public Func<TEntity, bool>? Predicate { get; set; }
}
