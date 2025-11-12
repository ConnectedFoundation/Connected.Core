namespace Connected.Graphics;

public enum GlyphFamily
{
	Default = 0,
	Thin = 1,
	Light = 2,
	Solid = 3
}

public class Glyph
{
	public string? Type { get; set; }
	public GlyphFamily Family { get; set; } = GlyphFamily.Default;
}