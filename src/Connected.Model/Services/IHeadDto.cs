namespace Connected.Services;
public interface IHeadDto<T> : IDto
{
	T? Head { get; set; }
}
