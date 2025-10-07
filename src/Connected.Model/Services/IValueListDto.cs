namespace Connected.Services;
public interface IValueDto<TPrimaryKey> : IDto
{
	TPrimaryKey Value { get; set; }
}
