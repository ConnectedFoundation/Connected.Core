using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Visitors;
using Connected.Reflection;
using Connected.Storage;
using System.Data;
using System.Linq.Expressions;
using System.Text.Json;

namespace Connected.Data.Expressions.Evaluation;

public sealed class ExecutionBuilder
	: DatabaseVisitor
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

	public static Expression<Func<IStorageExecutor, Task<object>>> Build(ExpressionCompilationContext context, Linguist linguist, Expression expression)
	{
		var executor = Expression.Parameter(typeof(IStorageExecutor), "executor");
		var builder = new ExecutionBuilder(context, linguist, executor);

		return builder.Build(expression);
	}

	private Expression<Func<IStorageExecutor, Task<object>>> Build(Expression expression)
	{
		expression = Visit(expression);

		if (expression is not MethodCallExpression call || !IsTaskType(call.Type))
			expression = Expression.Call(typeof(Task), nameof(Task.FromResult), [expression.Type], expression);

		var taskResultType = expression.Type.GetGenericArguments().Single();
		var result = Expression.Call(typeof(ExecutionBuilder), nameof(ConvertToObjectTask), [taskResultType], expression);
		expression = Expression.Lambda<Func<IStorageExecutor, Task<object>>>(result, (ParameterExpression)Executor);

		return (Expression<Func<IStorageExecutor, Task<object>>>)expression;
	}

	private static bool IsTaskType(Type type)
	{
		return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);
	}

	private static async Task<object> ConvertToObjectTask<TResult>(Task<TResult> task)
	{
		var value = await task;

		return value is null ? throw new NullReferenceException(nameof(value)) : value;
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
			var dbType = parameter.Value.Type.ToDbType();
			object? value = parameter.Value.Value;

			if (dbType == DbType.Object && value != null)
				value = JsonSerializer.Serialize(value);

			command.Parameters.Add(new StorageParameter
			{
				Direction = ParameterDirection.Input,
				Name = $"@{parameter.Key}",
				Type = dbType,
				Value = value
			});
		}


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
		/*
		 * call low-level execute directly on supplied DbQueryProvider
		 */
		Type typeArgument;

		if (projection.Type.IsEnumerable())
			typeArgument = projection.Type.GetGenericArguments()[0];
		else
			typeArgument = projection.Type;

		var constant = Expression.Constant(operation);
		Expression body = Expression.Call(Executor, nameof(IStorageExecutor.Execute), [typeArgument], constant);

		return body;
	}
}
