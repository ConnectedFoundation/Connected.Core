using Connected.Data.Expressions.Collections;
using System.Collections.ObjectModel;

namespace Connected.Data.Expressions.Expressions;

public sealed class DeclarationExpression(IEnumerable<VariableDeclaration> variables, SelectExpression source)
		: CommandExpression(DatabaseExpressionType.Declaration, typeof(void))
{
	public ReadOnlyCollection<VariableDeclaration> Variables { get; } = variables.ToReadOnly();
	public SelectExpression Source { get; } = source;
}
