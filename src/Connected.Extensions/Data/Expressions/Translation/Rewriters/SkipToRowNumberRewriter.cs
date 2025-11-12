using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Visitors;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Rewriters;

public sealed class SkipToRowNumberRewriter : DatabaseVisitor
{
	private SkipToRowNumberRewriter(QueryLanguage language)
	{
		Language = language;
	}

	private QueryLanguage Language { get; set; }

	public static Expression Rewrite(QueryLanguage language, Expression expression)
	{
		if (new SkipToRowNumberRewriter(language).Visit(expression) is not Expression skipToRowExpression)
			throw new NullReferenceException(nameof(skipToRowExpression));

		return skipToRowExpression;
	}

	protected override Expression VisitSelect(SelectExpression expression)
	{
		expression = (SelectExpression)base.VisitSelect(expression);

		if (expression.Skip is not null)
		{
			var newSelect = expression.SetSkip(null).SetTake(null);
			var canAddColumn = !expression.IsDistinct && (expression.GroupBy is null || expression.GroupBy.Count == 0);

			if (!canAddColumn)
				newSelect = newSelect.AddRedundantSelect(Language, Alias.New());

			var colType = Language.TypeSystem.ResolveColumnType(typeof(int));

			newSelect = newSelect.AddColumn(new ColumnDeclaration("_rownum", new RowNumberExpression(expression.OrderBy), colType));
			newSelect = newSelect.AddRedundantSelect(Language, Alias.New());
			newSelect = newSelect.RemoveColumn(newSelect.Columns.Single(c => c.Name == "_rownum"));

			if (newSelect.From is not SelectExpression se)
				throw new NullReferenceException(SR.ErrExpectedExpression);

			var newAlias = se.Alias;
			var rnCol = new ColumnExpression(typeof(int), colType, newAlias, "_rownum");

			Expression where;

			if (expression.Take is not null)
				where = new BetweenExpression(rnCol, Expression.Add(expression.Skip, Expression.Constant(1)), Expression.Add(expression.Skip, expression.Take));
			else
				where = rnCol.GreaterThan(expression.Skip);

			if (newSelect.Where != null)
				where = newSelect.Where.And(where);

			newSelect = newSelect.SetWhere(where);
			expression = newSelect;
		}

		return expression;
	}
}