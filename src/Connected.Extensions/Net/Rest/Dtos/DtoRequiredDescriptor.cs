namespace Connected.Net.Rest.Dtos;

internal sealed class DtoRequiredDescriptor
{
	public bool IsRequired { get; set; }
	public string? ErrorMessage { get; set; }
}