using Connected.Data.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.TypeSystem;

namespace Connected.Storage.Sql.Query;

internal sealed class TSqlLanguage
	: QueryLanguage
{
	private static TSqlLanguage? _default;

	static TSqlLanguage()
	{
		SplitChars = ['.'];
	}

	public TSqlLanguage()
	{
		TypeSystem = new SqlTypeSystem();
	}

	public override QueryTypeSystem TypeSystem { get; }
	private static char[] SplitChars { get; }
	public override bool AllowsMultipleCommands => true;
	public override bool AllowSubqueryInSelectWithoutFrom => true;
	public override bool AllowDistinctInAggregates => true;

	public static TSqlLanguage Default
	{
		get
		{
			if (_default is null)
				Interlocked.CompareExchange(ref _default, new TSqlLanguage(), null);

			return _default;
		}
	}

	public override string Quote(string name)
	{
		if (name.StartsWith('[') && name.EndsWith(']'))
			return name;
		else if (name.Contains('.'))
			return $"[{string.Join("].[", name.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries))}]";
		else
			return $"[{name}]";
	}

	public override Linguist CreateLinguist(ExpressionCompilationContext context, Translator translator)
	{
		return new TSqlLinguist(context, this, translator);
	}
}