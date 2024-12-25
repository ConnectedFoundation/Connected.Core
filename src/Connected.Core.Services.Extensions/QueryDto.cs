namespace Connected.Services;

public class QueryDto : Dto, IQueryDto
{
	public QueryDto()
	{
		OrderBy = [];
		Paging = new PagingOptions();
	}

	public List<IOrderByDescriptor> OrderBy { get; set; }
	public IPagingOptions Paging { get; set; }

	public static QueryDto Default => new()
	{
		Paging = new PagingOptions
		{
			Index = 0,
			Size = 10
		}
	};

	public static QueryDto NoPaging => new()
	{
		Paging = new PagingOptions
		{
			Index = 0,
			Size = int.MaxValue
		}
	};
}
