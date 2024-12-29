using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Projections;

internal sealed class ProjectedColumns
{
	private readonly Expression _projector;
	private readonly ReadOnlyCollection<ColumnDeclaration> _columns;

	public ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
	{
		_projector = projector;
		_columns = columns;
	}

	public Expression Projector => _projector;
	public ReadOnlyCollection<ColumnDeclaration> Columns => _columns;

}
