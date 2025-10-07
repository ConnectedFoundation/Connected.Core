namespace Connected.Services.Search;

public interface ISearchDto : IDto
{
	IPaging Paging { get; set; }
	string Text { get; set; }
}
