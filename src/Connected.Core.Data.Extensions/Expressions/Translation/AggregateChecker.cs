using System.Linq.Expressions;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation;

public sealed class AggregateChecker : DatabaseVisitor
{
	private AggregateChecker()
	{

	}

	private bool HasAggregate { get; set; }

	public static bool HasAggregates(SelectExpression expression)
	{
		var checker = new AggregateChecker();

		checker.Visit(expression);

		return checker.HasAggregate;
	}

	protected override Expression VisitAggregate(AggregateExpression aggregate)
	{
		HasAggregate = true;

		return aggregate;
	}

	protected override Expression VisitSelect(SelectExpression select)
	{
		Visit(select.Where);
		VisitOrderBy(select.OrderBy);
		VisitColumnDeclarations(select.Columns);

		return select;
	}

	protected override Expression VisitSubquery(SubqueryExpression subquery) => subquery;
}