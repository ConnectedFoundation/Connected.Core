using Connected.Services;

namespace Connected.Net.Dtos;

public interface IRequestArgumentDto : IDto
{
	string Property { get; set; }
	object? Value { get; set; }
}
