namespace Connected.Startup.Cors;
internal sealed class CorsConfiguration
{
	public bool Enabled { get; set; }
	public string? Origins { get; set; }
}
