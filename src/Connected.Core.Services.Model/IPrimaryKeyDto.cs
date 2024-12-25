namespace Connected.Services;

public interface IPrimaryKeyDto<T> : IDto
{
	public T? Id { get; set; }
}
