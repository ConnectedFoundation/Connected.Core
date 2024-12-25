namespace Connected.Services;

public interface IQueryDto : IDto
{
	List<IOrderByDescriptor> OrderBy { get; set; }
	IPagingOptions Paging { get; set; }
}
