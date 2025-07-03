namespace Connected.Services;
public interface IParentDto<T> : IDto
{
	T? Parent { get; set; }
}
