namespace Connected.Data.Expressions.Collections;

internal interface IDeferLoadable
{
	bool IsLoaded { get; }
	void Load();
}
