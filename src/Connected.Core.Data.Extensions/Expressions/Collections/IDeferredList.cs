using System.Collections;
using System.Collections.Generic;

namespace Connected.Data.Expressions.Collections;

internal interface IDeferredList : IList, IDeferLoadable
{
}

internal interface IDeferredList<T> : IList<T>, IDeferredList
{
}
