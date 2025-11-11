using System.Collections;

namespace Connected.Data.Expressions.Collections;

internal interface IDeferredList
	: IList, IDeferLoadable
{
}

internal interface IDeferredList<T> : IList<T>, IDeferredList
{
}
