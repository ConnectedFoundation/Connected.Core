using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Translation.Projections;

internal sealed class ProjectedColumns(Expression? projector, ReadOnlyCollection<ColumnDeclaration> columns)
{
	public Expression? Projector => projector;
	public ReadOnlyCollection<ColumnDeclaration> Columns => columns;
}
