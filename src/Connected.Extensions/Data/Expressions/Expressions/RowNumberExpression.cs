using Connected.Data.Expressions.Collections;
using System.Collections.ObjectModel;

namespace Connected.Data.Expressions.Expressions;

public sealed class RowNumberExpression(IEnumerable<OrderExpression>? orderBy)
		: DatabaseExpression(DatabaseExpressionType.RowCount, typeof(int))
{
	public ReadOnlyCollection<OrderExpression> OrderBy { get; } = orderBy is null ? new List<OrderExpression>().AsReadOnly() : orderBy.ToReadOnly();
}