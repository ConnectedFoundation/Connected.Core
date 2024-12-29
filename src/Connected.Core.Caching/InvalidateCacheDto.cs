using Connected.Services;

namespace Connected.Caching;
internal sealed class InvalidateCacheDto : Dto
{
	public string? Id { get; set; }
	public string? Key { get; set; }
}