using Connected.Annotations;
using Connected.Data.Expressions;
using Connected.Data.Expressions.Evaluation;
using Connected.Data.Expressions.Query;
using Connected.Data.Expressions.Translation;
using Connected.Entities;
using Connected.Services;
using Connected.Storage.Sql.Query;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Storage.Sql;

[Priority(0)]
internal sealed class SqlStorage : QueryProvider, IStorageExecutor, IQueryMiddleware
{
	public SqlStorage(IStorageProvider storage)
	{
		Storage = storage;
	}

	private IStorageProvider Storage { get; }
	private StorageConnectionMode ConnectionMode { get; set; } = StorageConnectionMode.Shared;

	public Task<bool> Invoke(Type type, StorageConnectionMode connectionMode)
	{
		ConnectionMode = connectionMode;

		return Task.FromResult(true);
	}

	protected override object? OnExecute(Expression expression)
	{
		return CreateExecutionPlan(expression).Compile()(this);
	}

	private static Expression<Func<IStorageExecutor, object>> CreateExecutionPlan(Expression expression)
	{
		var lambda = expression as LambdaExpression;

		if (lambda is not null)
			expression = lambda.Body;

		var context = new ExpressionCompilationContext(new TSqlLanguage());
		var translator = new Translator(context);
		var translation = translator.Translate(expression);
		var parameters = lambda?.Parameters;
		var provider = Resolve(expression, parameters, typeof(IStorage<>));

		if (provider is null)
		{
			var rootQueryable = Resolve(expression, parameters, typeof(IQueryable));

			provider = Expression.Property(rootQueryable, typeof(IQueryable).GetTypeInfo().GetDeclaredProperty(nameof(IQueryable.Provider)));
		}

		return ExecutionBuilder.Build(context, new TSqlLinguist(context, TSqlLanguage.Default, translator), translation, provider);
	}

	/// <summary>
	/// Find the expression of the specified type, either in the specified expression or parameters.
	/// </summary>
	private static Expression Resolve(Expression expression, IList<ParameterExpression> parameters, Type type)
	{
		if (parameters is not null)
		{
			var found = parameters.FirstOrDefault(p => type.IsAssignableFrom(p.Type));

			if (found is not null)
				return found;
		}

		return SubtreeResolver.Resolve(expression, type);
	}

	public IEnumerable<TResult?> Execute<TResult>(IStorageOperation operation)
		 where TResult : IEntity
	{
		var dto = Dto.Factory.Create<IStorageContextDto>();

		dto.Operation = operation;
		dto.ConnectionMode = ConnectionMode;

		return Storage.Open<TResult>(ConnectionMode).Query(dto).Result;
	}
}
