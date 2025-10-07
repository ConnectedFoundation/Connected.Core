namespace Connected.Services;
public interface IHeadListDto<T> : IDto
{
	List<T> Items { get; set; }
}
