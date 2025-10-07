using System.Collections.Generic;
using System.Collections.ObjectModel;
using Connected.Data.Expressions.Collections;

namespace Connected.Data.Expressions.Expressions;

public sealed class RowNumberExpression : DatabaseExpression
{
	public RowNumberExpression(IEnumerable<OrderExpression>? orderBy)
		  : base(DatabaseExpressionType.RowCount, typeof(int))
	{
		OrderBy = orderBy is null ? new List<OrderExpression>().AsReadOnly() : orderBy.ToReadOnly();
	}

	public ReadOnlyCollection<OrderExpression> OrderBy { get; }
}