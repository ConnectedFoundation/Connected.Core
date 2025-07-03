namespace Connected.Services;

public interface ITagListDto : IDto
{
	List<string> Items { get; set; }
}
