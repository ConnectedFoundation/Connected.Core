using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Visitors;
using Connected.Reflection;
using Connected.Storage;
using System.Data;
using System.Linq.Expressions;

namespace Connected.Data.Expressions.Evaluation;

public sealed class ExecutionBuilder : DatabaseVisitor
{
	private ExecutionBuilder(ExpressionCompilationContext context, Linguist linguist, Expression executor)
	{
		Context = context;
		Linguist = linguist;
		Executor = executor;
	}

	private ExpressionCompilationContext Context { get; }
	private Linguist Linguist { get; }
	private Expression Executor { get; }

	public static Expression<Func<IStorageExecutor, object>> Build(ExpressionCompilationContext context, Linguist linguist, Expression expression, Expression provider)
	{
		var executor = Expression.Parameter(typeof(IStorageExecutor), "executor");
		var builder = new ExecutionBuilder(context, linguist, executor);

		return builder.Build(expression);
	}

	private Expression<Func<IStorageExecutor, object>> Build(Expression expression)
	{
		expression = Visit(expression);
		expression = Expression.Lambda<Func<IStorageExecutor, object>>(expression, (ParameterExpression)Executor);

		return (Expression<Func<IStorageExecutor, object>>)expression;
	}

	protected override Expression VisitProjection(ProjectionExpression projection)
	{
		/*
		 * parameterize query
		 */
		var commandText = Linguist.Format(projection.Select);
		var command = new StorageOperation
		{
			CommandText = commandText
		};

		foreach (var parameter in Context.Parameters)
		{
			command.Parameters.Add(new StorageParameter
			{
				Direction = ParameterDirection.Input,
				Name = $"@{parameter.Key}",
				Type = parameter.Value.Type.ToDbType(),
				Value = parameter.Value.Value
			});
		}
		;

		foreach (var variable in Context.Variables)
		{
			command.Variables.Add(new StorageVariable
			{
				Name = variable.Key,
				Values = variable.Value
			});
		}

		return ExecuteProjection(projection, command);
	}

	private Expression ExecuteProjection(ProjectionExpression projection, IStorageOperation operation)
	{
		var method = nameof(IStorageExecutor.Execute);
		/*
	  * call low-level execute directly on supplied DbQueryProvider
	  */
		Type typeArgument;

		if (projection.Type.IsEnumerable())
			typeArgument = projection.Type.GetGenericArguments()[0];
		else
			typeArgument = projection.Type;

		var constant = Expression.Constant(operation);
		Expression body = Expression.Call(Executor, nameof(IStorageExecutor.Execute), new Type[] { typeArgument }, constant);

		if (projection.Aggregator is not null)// apply aggregator
			body = ExpressionReplacer.Replace(projection.Aggregator.Body, projection.Aggregator.Parameters[0], body);

		return body;
	}
}
