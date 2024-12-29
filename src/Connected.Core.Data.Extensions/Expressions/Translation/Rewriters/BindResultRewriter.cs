using System.Collections.Generic;
using System.Collections.ObjectModel;
using Connected.Data.Expressions.Expressions;

namespace Connected.Data.Expressions.Translation.Rewriters;

public sealed class BindResultRewriter
{
	public BindResultRewriter(IEnumerable<ColumnDeclaration> columns, IEnumerable<OrderExpression> orderings)
	{
		Columns = columns as ReadOnlyCollection<ColumnDeclaration>;

		Columns ??= new List<ColumnDeclaration>(columns).AsReadOnly();

		Orderings = orderings as ReadOnlyCollection<OrderExpression>;

		Orderings ??= new List<OrderExpression>(orderings).AsReadOnly();
	}

	public ReadOnlyCollection<ColumnDeclaration>? Columns { get; private set; }
	public ReadOnlyCollection<OrderExpression>? Orderings { get; private set; }
}