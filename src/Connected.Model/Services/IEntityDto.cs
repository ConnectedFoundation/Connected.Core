namespace Connected.Services;
public interface IEntityDto : IDto
{
	string Entity { get; set; }

	string EntityId { get; set; }
}
