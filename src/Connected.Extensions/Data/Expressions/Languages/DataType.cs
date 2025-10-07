namespace Connected.Data.Expressions.Languages;

public abstract class DataType
{
	public bool NotNull { get; protected set; }
	public int Length { get; protected set; }
	public short Precision { get; protected set; }
	public short Scale { get; protected set; }
}
