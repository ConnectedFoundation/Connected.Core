using Connected.Net.Dtos;
using Connected.Services;

namespace Connected.Net.Rest;

internal sealed class RequestArgumentDto : Dto, IRequestArgumentDto
{
	public required string Property { get; set; }
	public object? Value { get; set; }
}
