using System.Collections.Generic;
using System.Collections.ObjectModel;
using Connected.Data.Expressions.Collections;

namespace Connected.Data.Expressions.Expressions;

public sealed class DeclarationExpression : CommandExpression
{
	public DeclarationExpression(IEnumerable<VariableDeclaration> variables, SelectExpression source)
		  : base(DatabaseExpressionType.Declaration, typeof(void))
	{
		Variables = variables.ToReadOnly();
		Source = source;
	}

	public ReadOnlyCollection<VariableDeclaration> Variables { get; }
	public SelectExpression Source { get; }
}
