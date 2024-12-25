namespace Connected.Services;
public interface IValueDto<T> : IDto
{
	T Value { get; set; }
}
