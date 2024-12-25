namespace Connected.Services;
public interface IHeadDto<T> : IDto
{
	public T? Head { get; set; }
}
