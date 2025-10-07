namespace Connected.Net.Rest.Dtos;

internal sealed class DtoPropertyDescriptor
{
	public DtoPropertyDescriptor()
	{
		Required = new();
		MinLength = new();
		MaxLength = new();
		MinValue = new();
		MaxValue = new();
		Email = new();
	}

	public string? Name { get; set; }
	public string? Type { get; set; }
	public string? Description { get; set; }
	public string? Text { get; set; }

	public bool IsPassword { get; set; }

	public DtoRequiredDescriptor Required { get; }
	public DtoMaxLengthDescriptor MinLength { get; }
	public DtoMaxLengthDescriptor MaxLength { get; }
	public DtoMinValueDescriptor MinValue { get; }
	public DtoMaxValueDescriptor MaxValue { get; }
	public DtoEmailDescriptor Email { get; }
}