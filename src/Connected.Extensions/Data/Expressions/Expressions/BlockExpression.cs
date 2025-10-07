using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq.Expressions;
using Connected.Data.Expressions.Collections;

namespace Connected.Data.Expressions.Expressions;

public sealed class BlockExpression : CommandExpression
{
	public BlockExpression(IList<Expression> commands)
		  : base(DatabaseExpressionType.Block, commands[commands.Count - 1].Type)
	{
		Commands = commands.ToReadOnly();
	}

	public BlockExpression(params Expression[] commands)
		  : this((IList<Expression>)commands)
	{
	}

	public ReadOnlyCollection<Expression> Commands { get; }
}
