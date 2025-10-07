using System.Collections.Generic;
using System;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Expressions;

public sealed class BatchExpression : Expression
{
	public BatchExpression(Expression input, LambdaExpression operation, Expression batchSize, Expression stream)
	{
		Input = input;
		Operation = operation;
		BatchSize = batchSize;
		Stream = stream;
		Type = typeof(IEnumerable<>).MakeGenericType(operation.Body.Type);
	}

	public override Type Type { get; }
	public Expression Input { get; }
	public LambdaExpression Operation { get; }
	public Expression BatchSize { get; }
	public Expression Stream { get; }

	public override ExpressionType NodeType => (ExpressionType)DatabaseExpressionType.Batch;
}