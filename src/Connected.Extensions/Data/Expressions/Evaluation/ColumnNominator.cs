using System.Linq.Expressions;
using System.Collections.Generic;
using ExpressionVisitor = Connected.Data.Expressions.Visitors.ExpressionVisitor;
using Connected.Data.Expressions.Mappings;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class ColumnNominator : ExpressionVisitor
{
	private ColumnNominator()
	{
		Candidates = new HashSet<Expression>();
	}

	private HashSet<Expression> Candidates { get; set; }
	private bool CannotBeEvaluated { get; set; }

	public static HashSet<Expression> Nominate(Expression expression)
	{
		var nominator = new ColumnNominator();

		nominator.Visit(expression);

		return nominator.Candidates;
	}

	protected override Expression? Visit(Expression? expression)
	{
		if (expression is not null)
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
		}

		return expression;
	}
}