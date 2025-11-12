using Connected.Annotations;
using Connected.Data.Expressions;
using Connected.Data.Expressions.Evaluation;
using Connected.Data.Expressions.Query;
using Connected.Data.Expressions.Translation;
using Connected.Entities;
using Connected.Services;
using Connected.Storage.Oracle.Query;
using System.Linq.Expressions;

namespace Connected.Storage.Oracle;

/// <summary>
/// Provides Oracle database query provider and storage execution capabilities.
/// </summary>
/// <remarks>
/// This sealed class implements both <see cref="IStorageExecutor"/> and <see cref="IQueryMiddleware"/>
/// to provide LINQ query translation and execution against Oracle databases. It extends
/// <see cref="QueryProvider"/> to handle expression tree compilation and translation using
/// Oracle-specific linguist and formatter. The class serves as the bridge between LINQ
/// expressions written in application code and executable Oracle SQL statements with proper
/// bind variable handling (colon-prefix), double-quote identifier escaping, and Oracle-specific
/// function mapping. It manages connection modes (Shared or Isolated) and delegates actual query
/// execution to the underlying storage provider while ensuring proper parameter binding and result
/// materialization. The implementation uses priority 0 to ensure it processes queries before other
/// middleware. Oracle connections are managed through Oracle.ManagedDataAccess.Core connection pooling.
/// </remarks>
[Priority(0)]
internal sealed class OracleStorage(IStorageProvider storage)
		: QueryProvider, IStorageExecutor, IQueryMiddleware
{
	/// <summary>
	/// Gets the storage provider for executing database operations.
	/// </summary>
	private IStorageProvider Storage { get; } = storage;

	/// <summary>
	/// Gets or sets the connection mode for storage operations.
	/// </summary>
	/// <value>
	/// The connection mode determining whether to use shared or isolated database connections.
	/// Defaults to <see cref="StorageConnectionMode.Shared"/>.
	/// </value>
	private StorageConnectionMode ConnectionMode { get; set; } = StorageConnectionMode.Shared;

	/// <summary>
	/// Invokes the middleware to configure connection mode for the specified entity type.
	/// </summary>
	/// <param name="type">The entity type that will be queried.</param>
	/// <param name="connectionMode">The connection mode to use for operations.</param>
	/// <returns>A task that represents the asynchronous operation. The task result is always <c>true</c>.</returns>
	/// <remarks>
	/// This method is called by the middleware pipeline to configure connection mode before
	/// query execution. It stores the connection mode for use during query execution. Shared
	/// mode reuses connections from the Oracle connection pool; Isolated mode creates dedicated
	/// connections for each operation.
	/// </remarks>
	public Task<bool> Invoke(Type type, StorageConnectionMode connectionMode)
	{
		ConnectionMode = connectionMode;

		return Task.FromResult(true);
	}

	/// <summary>
	/// Executes the specified expression by compiling it into an execution plan.
	/// </summary>
	/// <param name="expression">The expression tree to execute.</param>
	/// <returns>The result of executing the expression.</returns>
	/// <remarks>
	/// This method creates an execution plan from the expression tree, compiles it into
	/// a delegate, and invokes it with this storage executor as the context. The execution
	/// plan handles translation from LINQ expressions to Oracle SQL statements with proper
	/// bind variables, identifiers, and Oracle-specific functions.
	/// </remarks>
	protected override object OnExecute(Expression expression)
	{
		return CreateExecutionPlan(expression).Compile()(this);
	}

	/// <summary>
	/// Creates an execution plan for the specified expression using Oracle query translation.
	/// </summary>
	/// <param name="expression">The expression tree to create an execution plan for.</param>
	/// <returns>
	/// A lambda expression that takes an <see cref="IStorageExecutor"/> and returns the execution result.
	/// </returns>
	/// <remarks>
	/// This method performs the following steps:
	/// 1. Extracts the expression body if it's a lambda expression
	/// 2. Creates an Oracle-specific compilation context with <see cref="OracleLanguage"/>
	/// 3. Translates the expression using the translator
	/// 4. Builds an execution plan using <see cref="OracleLinguist"/> for SQL formatting
	/// The resulting execution plan generates Oracle SQL with colon-prefixed bind variables (:param),
	/// double-quoted identifiers ("schema"."table"), and Oracle-specific functions (SUBSTR, INSTR,
	/// LENGTH, TO_DATE, NVL, etc.).
	/// </remarks>
	private static Expression<Func<IStorageExecutor, object>> CreateExecutionPlan(Expression expression)
	{
		var lambda = expression as LambdaExpression;

		if (lambda is not null)
			expression = lambda.Body;

		/*
		 * Create Oracle-specific compilation context with language configuration
		 */
		var context = new ExpressionCompilationContext(new OracleLanguage());

		/*
		 * Create translator and translate the expression tree
		 */
		var translator = new Translator(context);
		var translation = translator.Translate(expression);

		/*
		 * Build the execution plan using Oracle linguist for SQL generation
		 */
		return ExecutionBuilder.Build(context, new OracleLinguist(context, OracleLanguage.Default, translator), translation);
	}

	/// <summary>
	/// Finds the expression of the specified type, either in the specified expression or parameters.
	/// </summary>
	/// <param name="expression">The expression tree to search.</param>
	/// <param name="parameters">The list of parameter expressions to search.</param>
	/// <param name="type">The type to search for.</param>
	/// <returns>The found expression, or <c>null</c> if not found.</returns>
	/// <remarks>
	/// This method first searches through the provided parameters for a match, then falls back
	/// to resolving the type from the expression subtree using <see cref="SubtreeResolver"/>.
	/// </remarks>
	private static Expression? Resolve(Expression expression, IList<ParameterExpression> parameters, Type type)
	{
		if (parameters is not null)
		{
			/*
			 * Search parameters first for matching type
			 */
			var found = parameters.FirstOrDefault(p => type.IsAssignableFrom(p.Type));

			if (found is not null)
				return found;
		}

		/*
		 * Fall back to subtree resolution
		 */
		return SubtreeResolver.Resolve(expression, type);
	}

	/// <summary>
	/// Executes a storage operation and returns the results as an enumerable sequence.
	/// </summary>
	/// <typeparam name="TResult">The entity type of the query results.</typeparam>
	/// <param name="operation">The storage operation containing the Oracle SQL command with bind variables and parameters.</param>
	/// <returns>An enumerable sequence of entities returned by the query.</returns>
	/// <remarks>
	/// This method creates a storage context DTO with the operation and connection mode,
	/// then delegates execution to the underlying storage provider. The storage provider
	/// handles actual Oracle database communication, bind variable parameter binding, and
	/// result materialization with Oracle-specific type conversions (NUMBER to numeric types,
	/// VARCHAR2/CLOB to strings, RAW/BLOB to byte arrays). Results are returned synchronously
	/// as an enumerable sequence. Oracle connection pooling ensures efficient connection reuse.
	/// </remarks>
	public IEnumerable<TResult?> Execute<TResult>(IStorageOperation operation)
		 where TResult : IEntity
	{
		/*
		 * Create storage context DTO with operation configuration
		 */
		var dto = Dto.Factory.Create<IStorageContextDto>();

		dto.Operation = operation;
		dto.ConnectionMode = ConnectionMode;

		/*
		 * Execute query through storage provider and return Oracle results
		 */
		return Storage.Open<TResult>(ConnectionMode).Query(dto).Result;
	}
}
