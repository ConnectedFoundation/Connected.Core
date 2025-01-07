using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Storage.Sharding.Dtos;
internal class QueryShardsDto : Dto, IQueryShardsDto
{
	[Required, MaxLength(1024)]
	public required string Entity { get; set; }
}
