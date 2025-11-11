using Connected.Data.Expressions.Serialization;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

/// <summary>
/// Represents the different kinds of database related expression nodes used by the
/// expression model. These values start at 1000 to avoid colliding with values in
/// <see cref="ExpressionType"/> from <see cref="System.Linq.Expressions"/>.
/// </summary>
public enum DatabaseExpressionType
{
	/// <summary>Represents a table reference.</summary>
	Table = 1000,
	/// <summary>Represents a client-side join (non-database join).</summary>
	ClientJoin = 1001,
	/// <summary>Represents a column reference.</summary>
	Column = 1002,
	/// <summary>Represents a SELECT statement.</summary>
	Select = 1003,
	/// <summary>Represents a projection within a query.</summary>
	Projection = 1004,
	/// <summary>Represents an entity reference.</summary>
	Entity = 1005,
	/// <summary>Represents a join between two sources.</summary>
	Join = 1006,
	/// <summary>Represents an aggregate operation (SUM, COUNT, etc.).</summary>
	Aggregate = 1007,
	/// <summary>Represents a scalar expression.</summary>
	Scalar = 1008,
	/// <summary>Represents an EXISTS subquery.</summary>
	Exists = 1009,
	/// <summary>Represents an IN expression.</summary>
	In = 1010,
	/// <summary>Represents a grouping clause.</summary>
	Grouping = 1011,
	/// <summary>Represents an aggregate subquery.</summary>
	AggregateSubquery = 1012,
	/// <summary>Represents an IS NULL check.</summary>
	IsNull = 1013,
	/// <summary>Represents a BETWEEN expression.</summary>
	Between = 1014,
	/// <summary>Represents a row count operation (e.g. COUNT(*)).</summary>
	RowCount = 1015,
	/// <summary>Represents a named value (parameter).</summary>
	NamedValue = 1016,
	/// <summary>Marks an expression that is outer-joined.</summary>
	OuterJoined = 1017,
	/// <summary>Represents a batch of expressions.</summary>
	Batch = 1018,
	/// <summary>Represents a function call.</summary>
	Function = 1019,
	/// <summary>Represents a block of statements.</summary>
	Block = 1020,
	/// <summary>Represents a conditional (if) expression.</summary>
	If = 1021,
	/// <summary>Represents a declaration statement.</summary>
	Declaration = 1022,
	/// <summary>Represents a variable reference.</summary>
	Variable = 1023
}

/// <summary>
/// Base class for database-specific expression nodes.
/// </summary>
/// <remarks>
/// Database expressions map to the LINQ <see cref="Expression"/> model but use
/// a custom <see cref="DatabaseExpressionType"/> to identify node kinds. The
/// <see cref="NodeType"/> override exposes the underlying value as a numeric
/// <see cref="ExpressionType"/> so the expressions can participate in visitors
/// and tree walkers that expect <see cref="Expression"/> instances.
/// </remarks>
[DebuggerDisplay("{DebugText}")]
public abstract class DatabaseExpression(DatabaseExpressionType expressionType, Type type)
		: Expression
{
	/*
	 * store the CLR type for the expression (Expression.Type is abstract)
	 */
	private readonly Type _type = type;

	/// <summary>
	/// The specific database expression kind for this node.
	/// </summary>
	public DatabaseExpressionType ExpressionType { get; } = expressionType;

	/// <summary>
	/// Maps the custom <see cref="DatabaseExpressionType"/> into the numeric
	/// <see cref="ExpressionType"/> space. This allows consumers to inspect
	/// NodeType when traversing expression trees while still using distinct
	/// values for database expression kinds.
	/// </summary>
	public override ExpressionType NodeType => (ExpressionType)(int)ExpressionType;

	/// <summary>
	/// The runtime type of the expression's result.
	/// </summary>
	public override Type Type => _type;

	/*
	 * Text used by the debugger display attribute to provide a compact description.
	 */
	private string DebugText => $"{GetType().Name}: {this.ResolveNodeTypeName()} := {this}";

	/// <summary>
	/// Serializes the database expression into a textual representation using
	/// the <see cref="DatabaseSerializer"/>. This is useful for diagnostics and
	/// logging. Implementations should ensure the serializer understands the
	/// full expression graph.
	/// </summary>
	public override string ToString()
	{
		/*
		 * Delegate to the serializer to produce a stable textual representation
		 * of the expression tree. This helps when debugging or when generating
		 * SQL or other textual query languages from the expression model.
		 */
		return DatabaseSerializer.Serialize(this);
	}
}
