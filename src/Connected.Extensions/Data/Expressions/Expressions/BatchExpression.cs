using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class BatchExpression(Expression input, LambdaExpression operation, Expression batchSize, Expression stream)
		: Expression
{
	public override Type Type { get; } = typeof(IEnumerable<>).MakeGenericType(operation.Body.Type);
	public Expression Input { get; } = input;
	public LambdaExpression Operation { get; } = operation;
	public Expression BatchSize { get; } = batchSize;
	public Expression Stream { get; } = stream;

	public override ExpressionType NodeType => (ExpressionType)DatabaseExpressionType.Batch;
}