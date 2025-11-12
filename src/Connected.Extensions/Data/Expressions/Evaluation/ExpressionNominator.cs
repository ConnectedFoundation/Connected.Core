
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation.Projections;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Evaluation;

internal sealed class ExpressionNominator
	: DatabaseVisitor
{
	private ExpressionNominator(QueryLanguage language, ProjectionAffinity affinity)
	{
		Language = language;
		Affinity = affinity;

		Candidates = [];
	}

	private QueryLanguage Language { get; }
	private ProjectionAffinity Affinity { get; }

	private HashSet<Expression> Candidates { get; set; }
	private bool IsBlocked { get; set; }

	public static HashSet<Expression> Nominate(QueryLanguage language, ProjectionAffinity affinity, Expression expression)
	{
		var nominator = new ExpressionNominator(language, affinity);

		nominator.Visit(expression);

		return nominator.Candidates;
	}

	protected override Expression Visit(Expression expression)
	{
		var saveIsBlocked = IsBlocked;

		IsBlocked = false;

		if (Language.MustBeColumn(expression))
			Candidates.Add(expression);
		else
		{
			base.Visit(expression);

			if (!IsBlocked)
			{
				if (Language.MustBeColumn(expression) || Affinity == ProjectionAffinity.Server && Language.CanBeColumn(expression))
					Candidates.Add(expression);
				else
					IsBlocked = true;
			}

			IsBlocked |= saveIsBlocked;
		}

		return expression;
	}

	protected override Expression VisitProjection(ProjectionExpression expression)
	{
		Visit(expression.Projector);

		return expression;
	}
}