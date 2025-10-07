namespace Connected.Services;

public interface IPagingOptions
{
	int Size { get; set; }
	int Index { get; set; }
}
