namespace Connected.Data.Expressions.Translation;

public sealed class Alias
{
	private Alias() { }
	public override string ToString()
	{
		return $"A:{GetHashCode()}";
	}

	public static Alias New()
	{
		return new Alias();
	}
}
