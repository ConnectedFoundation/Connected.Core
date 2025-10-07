using System.Diagnostics;
using System;
using System.Linq.Expressions;
using Connected.Data.Expressions.Serialization;

namespace Connected.Data.Expressions.Expressions;

public enum DatabaseExpressionType
{
	Table = 1000,
	ClientJoin = 1001,
	Column = 1002,
	Select = 1003,
	Projection = 1004,
	Entity = 1005,
	Join = 1006,
	Aggregate = 1007,
	Scalar = 1008,
	Exists = 1009,
	In = 1010,
	Grouping = 1011,
	AggregateSubquery = 1012,
	IsNull = 1013,
	Between = 1014,
	RowCount = 1015,
	NamedValue = 1016,
	OuterJoined = 1017,
	Batch = 1018,
	Function = 1019,
	Block = 1020,
	If = 1021,
	Declaration = 1022,
	Variable = 1023
}

[DebuggerDisplay("{DebugText}")]
public abstract class DatabaseExpression : Expression
{
	private readonly Type _type;

	protected DatabaseExpression(DatabaseExpressionType expressionType, Type type)
	{
		ExpressionType = expressionType;
		_type = type;
	}
	public DatabaseExpressionType ExpressionType { get; }

	public override ExpressionType NodeType => (ExpressionType)(int)ExpressionType;
	public override Type Type => _type;

	private string DebugText => $"{GetType().Name}: {this.ResolveNodeTypeName()} := {this}";

	public override string ToString()
	{
		return DatabaseSerializer.Serialize(this);
	}
}
