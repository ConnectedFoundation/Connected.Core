namespace Connected.Data.Expressions.TypeSystem;

public abstract class QueryTypeSystem
{
	public abstract Languages.DataType Parse(string typeDeclaration);

	public abstract Languages.DataType ResolveColumnType(Type type);

	public abstract string Format(Languages.DataType type, bool suppressSize);
}
