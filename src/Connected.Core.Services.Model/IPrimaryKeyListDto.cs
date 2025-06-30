namespace Connected.Services;
public interface IPrimaryKeyListDto<T> : IDto
{
	List<T> Items { get; set; }
}
