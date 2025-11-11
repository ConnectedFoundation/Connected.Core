using Connected.Data.Expressions.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class BlockExpression(IList<Expression> commands)
		: CommandExpression(DatabaseExpressionType.Block, commands[commands.Count - 1].Type)
{
	public BlockExpression(params Expression[] commands)
		  : this((IList<Expression>)commands)
	{
	}

	public ReadOnlyCollection<Expression> Commands { get; } = commands.ToReadOnly();
}
