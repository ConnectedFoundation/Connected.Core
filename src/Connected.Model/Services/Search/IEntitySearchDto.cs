namespace Connected.Services.Search;

public interface IEntitySearchDto : ISearchDto
{
	string Entity { get; set; }

	string EntityId { get; set; }
}
