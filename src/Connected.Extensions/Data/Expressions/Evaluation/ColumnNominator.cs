using Connected.Data.Expressions.Mappings;
using System.Linq.Expressions;
using ExpressionVisitor = Connected.Data.Expressions.Visitors.ExpressionVisitor;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class ColumnNominator
	: ExpressionVisitor
{
	private ColumnNominator()
	{
		Candidates = [];
	}

	private HashSet<Expression> Candidates { get; set; }
	private bool CannotBeEvaluated { get; set; }

	public static HashSet<Expression> Nominate(Expression expression)
	{
		var nominator = new ColumnNominator();

		nominator.Visit(expression);

		return nominator.Candidates;
	}

	protected override Expression Visit(Expression expression)
	{
		var saveCannotBeEvaluated = CannotBeEvaluated;

		CannotBeEvaluated = false;

		base.Visit(expression);

		if (!CannotBeEvaluated)
		{
			if (MappingsCache.CanEvaluateLocally(expression))
				Candidates.Add(expression);
			else
				CannotBeEvaluated = true;
		}

		CannotBeEvaluated |= saveCannotBeEvaluated;

		return expression;
	}
}