namespace Connected.Net.Dtos;

public interface IBoundMessageAcknowledgeDto
	: IMessageAcknowledgeDto
{
	string Connection { get; set; }
}
