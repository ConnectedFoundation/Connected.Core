using Connected.Services;

namespace Connected.Net.Rest.Dtos;

public interface IRequestArgumentDto : IDto
{
	string Property { get; set; }
	object? Value { get; set; }
}
