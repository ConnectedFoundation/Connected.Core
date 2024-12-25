namespace Connected.Services;

public enum OrderByMode
{
	Ascending = 0,
	Descending = 1
}

public interface IOrderByDescriptor
{
	string Property { get; set; }
	OrderByMode Mode { get; set; }
}
