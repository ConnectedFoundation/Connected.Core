using System;
using System.Linq;
using System.Linq.Expressions;
using Connected.Data.Expressions.Translation.Resolvers;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Translation.Projections;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation.Rewriters;

public sealed class CrossApplyRewriter : DatabaseVisitor
{
	private CrossApplyRewriter(QueryLanguage language)
	{
		Language = language;
	}

	private QueryLanguage Language { get; }

	public static Expression Rewrite(QueryLanguage language, Expression expression)
	{
		if (new CrossApplyRewriter(language).Visit(expression) is not Expression crossApplyExpression)
			throw new NullReferenceException(nameof(crossApplyExpression));

		return crossApplyExpression;
	}

	protected override Expression VisitJoin(JoinExpression expression)
	{
		expression = (JoinExpression)base.VisitJoin(expression);

		if (expression.Join == JoinType.CrossApply || expression.Join == JoinType.OuterApply)
		{
			if (expression.Right is TableExpression)
				return new JoinExpression(JoinType.CrossJoin, expression.Left, expression.Right, null);
			else
			{
				var select = expression.Right as SelectExpression;

				if (select is not null && select.Take is null && select.Skip is null && !AggregateChecker.HasAggregates(select) && (select.GroupBy is null || !select.GroupBy.Any()))
				{
					var selectWithoutWhere = select.SetWhere(null);
					var referencedAliases = ReferencedAliasesResolver.Resolve(selectWithoutWhere);
					var declaredAliases = DeclaredAliasesResolver.Resolve(expression.Left);

					referencedAliases.IntersectWith(declaredAliases);

					if (!referencedAliases.Any())
					{
						var where = select.Where;

						select = selectWithoutWhere;

						var pc = ColumnProjector.ProjectColumns(Language, where, select.Columns, select.Alias, DeclaredAliasesResolver.Resolve(select.From));

						select = select.SetColumns(pc.Columns);
						where = pc.Projector;

						var jt = where == null ? JoinType.CrossJoin : expression.Join == JoinType.CrossApply ? JoinType.InnerJoin : JoinType.LeftOuter;

						return new JoinExpression(jt, expression.Left, select, where);
					}
				}
			}
		}

		return expression;
	}
}