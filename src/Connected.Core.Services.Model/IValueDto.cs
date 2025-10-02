namespace Connected.Services;
public interface IValueListDto<TPrimaryKey> : IDto
{
	List<TPrimaryKey> Items { get; set; }
}
