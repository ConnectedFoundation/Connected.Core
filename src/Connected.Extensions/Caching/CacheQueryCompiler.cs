using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Caching;

/// <summary>
/// Runs a query over a cache snapshot through a delegate compiled once for the query's shape.
/// </summary>
/// <remarks>
/// <para>
/// A cache query used to run through <see cref="EnumerableQuery{T}"/>, which rewrites and compiles its expression tree
/// on every execution - milliseconds of CPU for a query that then scans a few hundred cached rows, on every call. Two
/// queries built by the same line of code differ only in their values: the snapshot they run over and the closures
/// holding what the caller asked for. Those values become arguments, so the delegate compiled the first time a shape
/// is seen serves every later execution of it.
/// </para>
/// <para>
/// Only constants are lifted, never what is read through them: a closure stays a closure, and the members the query
/// reads from it are read when the query runs, exactly as before. The rewrite from <see cref="Queryable"/> to
/// <see cref="Enumerable"/> is the one <see cref="EnumerableQuery{T}"/> performs itself, so the compiled delegate is
/// the same code it compiled on every call.
/// </para>
/// <para>
/// Anything this does not handle - another queryable inside the query, a node kind outside LINQ, a method without an
/// enumerable counterpart, a result type it cannot produce - answers <see langword="false"/>, and the caller runs the
/// query the way it always has. That answer is remembered per shape. Setting the environment variable
/// <c>CONNECTED_CACHE_COMPILED_QUERIES</c> to <c>false</c> turns the whole mechanism off.
/// </para>
/// <para>
/// Every execution is counted on the <c>Connected.Caching</c> meter - <c>connected.cache.query.compiled</c> and
/// <c>connected.cache.query.fallback</c>, the latter tagged with its reason - so how much of a running service takes
/// each path can be read live, with <c>dotnet-counters monitor --counters Connected.Caching</c>.
/// </para>
/// </remarks>
internal static class CacheQueryCompiler
{
	/// <summary>The most shapes remembered; beyond it, new shapes run the way they always have.</summary>
	internal const int MaxShapes = 4096;

	private static readonly ConcurrentDictionary<ShapeKey, Shape> Shapes = new();
	private static readonly ConcurrentDictionary<MethodInfo, MethodInfo?> EnumerableMethods = new();
	private static readonly ConcurrentDictionary<string, long> FallbackReasons = new(StringComparer.Ordinal);
	private static readonly Meter Meter = new("Connected.Caching");
	private static readonly Counter<long> CompiledCounter = Meter.CreateCounter<long>("connected.cache.query.compiled",
		description: "Cache queries run through a delegate compiled once for their shape.");
	private static readonly Counter<long> FallbackCounter = Meter.CreateCounter<long>("connected.cache.query.fallback",
		description: "Cache queries run the way they always have, tagged with why.");
	private static long _compiled;
	private static long _fallback;

	/// <summary>A remembered shape: its delegate, or why it runs the way it always has.</summary>
	private readonly record struct Shape(Delegate? Run, string? Reason);

	/// <summary>Gets or sets whether queries run through compiled shapes.</summary>
	internal static bool Enabled { get; set; } =
		!string.Equals(Environment.GetEnvironmentVariable("CONNECTED_CACHE_COMPILED_QUERIES"), "false", StringComparison.OrdinalIgnoreCase);

	/// <summary>Gets how many shapes are remembered, compiled or declined.</summary>
	internal static int ShapeCount => Shapes.Count;

	/// <summary>Gets how many executions took the compiled path since the last <see cref="Reset"/>.</summary>
	internal static long CompiledExecutions => Interlocked.Read(ref _compiled);

	/// <summary>Gets how many executions ran the way they always have since the last <see cref="Reset"/>.</summary>
	internal static long FallbackExecutions => Interlocked.Read(ref _fallback);

	/// <summary>Gets the fallback executions by reason since the last <see cref="Reset"/>.</summary>
	internal static IReadOnlyDictionary<string, long> Fallbacks => new Dictionary<string, long>(FallbackReasons);

	/// <summary>Forgets every shape and count; for tests.</summary>
	internal static void Reset()
	{
		Shapes.Clear();
		FallbackReasons.Clear();
		Interlocked.Exchange(ref _compiled, 0);
		Interlocked.Exchange(ref _fallback, 0);
	}

	/// <summary>
	/// Runs a query over a snapshot through the delegate compiled for its shape.
	/// </summary>
	/// <typeparam name="TEntry">The cached entry type.</typeparam>
	/// <typeparam name="TResult">The type the caller asked the query to produce.</typeparam>
	/// <param name="expression">The query, rooted at the cache's own queryable.</param>
	/// <param name="source">The snapshot the query runs over.</param>
	/// <param name="result">The query's result, when it ran.</param>
	/// <returns><see langword="false"/> when the query must run the way it always has.</returns>
	/// <remarks>
	/// <para>
	/// A call walks the tree once, building nothing: it records the shape and collects the constants. The rewrite and
	/// the compilation happen only the first time a shape is seen.
	/// </para>
	/// <para>Exceptions thrown while the query runs are the query's own and propagate unchanged.</para>
	/// </remarks>
	public static bool TryExecute<TEntry, TResult>(Expression expression, IEnumerable<TEntry> source, out TResult result)
	{
		result = default!;

		if (!Enabled)
			return Fallback("disabled");

		var print = Fingerprint.ForThisThread(typeof(TEntry), typeof(TResult));

		try
		{
			print.Visit(expression);
		}
		catch
		{
			return Fallback("visit-error");
		}

		if (print.Unsupported)
			return Fallback(print.Reason ?? "unsupported");

		if (!Shapes.TryGetValue(ShapeKey.Probe(print.Tokens, print.Hash), out var shape))
		{
			shape = Build<TEntry, TResult>(expression, print.Arguments);

			if (Shapes.Count < MaxShapes)
				shape = Shapes.GetOrAdd(ShapeKey.Stored(print.Tokens, print.Hash), shape);
		}

		if (shape.Run is not Func<IEnumerable<TEntry>, object?[], TResult> run)
			return Fallback(shape.Reason ?? "not-compiled");

		/*
		 * Copied before the query runs: the walk belongs to this thread, and a query that runs another cache query on
		 * it would reuse it.
		 */
		var arguments = print.Arguments.ToArray();

		Interlocked.Increment(ref _compiled);
		CompiledCounter.Add(1);

		result = run(source, arguments);

		return true;
	}

	private static bool Fallback(string reason)
	{
		Interlocked.Increment(ref _fallback);
		FallbackReasons.AddOrUpdate(reason, 1, static (_, count) => count + 1);
		FallbackCounter.Add(1, new KeyValuePair<string, object?>("reason", reason));

		return false;
	}

	/// <summary>
	/// Rewrites a query the first time its shape is seen and compiles it.
	/// </summary>
	/// <param name="expression">The query.</param>
	/// <param name="collected">The constants the call's walk collected, in its order.</param>
	/// <remarks>
	/// The rewrite numbers constants in the order it meets them, and every later call passes them in the order its walk
	/// collected them; the two walks visit a tree the same way. That is checked here, over the same tree, constant by
	/// constant: a shape where they would disagree is declined rather than run with its values in the wrong places.
	/// </remarks>
	private static Shape Build<TEntry, TResult>(Expression expression, List<object?> collected)
	{
		try
		{
			var rewrite = new Rewriter(typeof(TEntry));
			var body = rewrite.Visit(expression)!;

			if (rewrite.Unsupported)
				return new(null, rewrite.Reason ?? "unsupported");

			if (rewrite.Arguments.Count != collected.Count)
				return new(null, "argument-order");

			for (var i = 0; i < collected.Count; i++)
			{
				if (!ReferenceEquals(rewrite.Arguments[i], collected[i]) && !Equals(rewrite.Arguments[i], collected[i]))
					return new(null, "argument-order");
			}

			if (body.Type != typeof(TResult))
			{
				if (!typeof(TResult).IsAssignableFrom(body.Type))
					return new(null, "result-type");

				body = Expression.Convert(body, typeof(TResult));
			}

			return new(Expression.Lambda<Func<IEnumerable<TEntry>, object?[], TResult>>(body, rewrite.Source, rewrite.Args).Compile(), null);
		}
		catch
		{
			return new(null, "compile-error");
		}
	}


	/// <summary>
	/// Finds the <see cref="Enumerable"/> method a <see cref="Queryable"/> method stands for: same name, same generic
	/// arity, and parameters that are the queryable ones with <c>IQueryable</c> read as <c>IEnumerable</c> and
	/// <c>Expression&lt;F&gt;</c> read as <c>F</c>.
	/// </summary>
	private static MethodInfo? EnumerableMethod(MethodInfo queryable) => EnumerableMethods.GetOrAdd(queryable, static method =>
	{
		var definition = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
		var parameters = definition.GetParameters();

		foreach (var candidate in typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static))
		{
			if (candidate.Name != definition.Name || candidate.IsGenericMethodDefinition != definition.IsGenericMethodDefinition)
				continue;

			if (definition.IsGenericMethodDefinition && candidate.GetGenericArguments().Length != definition.GetGenericArguments().Length)
				continue;

			var candidateParameters = candidate.GetParameters();

			if (candidateParameters.Length != parameters.Length)
				continue;

			var matches = true;

			for (var i = 0; i < parameters.Length && matches; i++)
				matches = Equivalent(parameters[i].ParameterType, candidateParameters[i].ParameterType);

			if (matches && Equivalent(definition.ReturnType, candidate.ReturnType))
				return method.IsGenericMethod ? candidate.MakeGenericMethod(method.GetGenericArguments()) : candidate;
		}

		return null;
	});

	private static bool Equivalent(Type queryable, Type enumerable)
	{
		if (queryable.IsGenericType && queryable.GetGenericTypeDefinition() == typeof(Expression<>))
			queryable = queryable.GetGenericArguments()[0];

		if (queryable.IsGenericParameter || enumerable.IsGenericParameter)
			return queryable.IsGenericParameter && enumerable.IsGenericParameter
				&& queryable.GenericParameterPosition == enumerable.GenericParameterPosition;

		if (queryable.IsArray || enumerable.IsArray)
			return queryable.IsArray && enumerable.IsArray && Equivalent(queryable.GetElementType()!, enumerable.GetElementType()!);

		if (!queryable.IsGenericType || !enumerable.IsGenericType)
			return queryable == enumerable;

		var queryableDefinition = queryable.GetGenericTypeDefinition();
		var enumerableDefinition = enumerable.GetGenericTypeDefinition();

		if (queryableDefinition == typeof(IQueryable<>))
			queryableDefinition = typeof(IEnumerable<>);
		else if (queryableDefinition == typeof(IOrderedQueryable<>))
			queryableDefinition = typeof(IOrderedEnumerable<>);

		if (queryableDefinition != enumerableDefinition)
			return false;

		var queryableArguments = queryable.GetGenericArguments();
		var enumerableArguments = enumerable.GetGenericArguments();

		for (var i = 0; i < queryableArguments.Length; i++)
		{
			if (!Equivalent(queryableArguments[i], enumerableArguments[i]))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Nodes outside LINQ over a cache, which neither walk takes on.
	/// </summary>
	private static bool Foreign(ExpressionType type) => type is ExpressionType.Extension or ExpressionType.Dynamic
		or ExpressionType.Block or ExpressionType.Loop or ExpressionType.Goto or ExpressionType.Label or ExpressionType.Switch
		or ExpressionType.Try or ExpressionType.RuntimeVariables or ExpressionType.DebugInfo or ExpressionType.Assign;

	/// <summary>
	/// Whether a constant is the cache's own queryable, which becomes the snapshot the query runs over.
	/// </summary>
	private static bool IsSource(object? value, Type entryType) => value is IQueryable queryable && queryable.ElementType == entryType
		&& value.GetType().IsAssignableTo(typeof(IQueryable<>).MakeGenericType(entryType));

	/// <summary>
	/// The walk every call makes: records the query's shape and collects its constants, building no nodes.
	/// </summary>
	/// <remarks>
	/// One per thread, reset for each call. The shape is a list of tokens - node kinds, types, members, methods, parameter
	/// positions - with a constant recorded only as the fact that a value stands there, hashed as it is written.
	/// </remarks>
	private sealed class Fingerprint : ExpressionVisitor
	{
		[ThreadStatic]
		private static Fingerprint? _thread;

		private static readonly object[] NodeTypes = [.. Enum.GetValues<ExpressionType>().Select(f => (object)f)];
		private static readonly object[] BindingTypes = [.. Enum.GetValues<MemberBindingType>().Select(f => (object)f)];
		private static readonly object[] Small = [.. Enumerable.Range(0, 64).Select(f => (object)f)];
		private static readonly object True = true, False = false;
		private static readonly object SourceToken = "source", ArgumentToken = "arg", ConditionalToken = "conditional";

		private readonly Dictionary<ParameterExpression, int> _parameters = [];
		private Type _entryType = typeof(object);
		private HashCode _hash;

		public List<object?> Tokens { get; } = [];
		public List<object?> Arguments { get; } = [];
		public bool Unsupported { get; private set; }
		public string? Reason { get; private set; }
		public int Hash => _hash.ToHashCode();

		public static Fingerprint ForThisThread(Type entryType, Type resultType)
		{
			var print = _thread ??= new Fingerprint();

			print._entryType = entryType;
			print._parameters.Clear();
			print._hash = new HashCode();
			print.Tokens.Clear();
			print.Arguments.Clear();
			print.Unsupported = false;
			print.Reason = null;
			print.Add(entryType);
			print.Add(resultType);

			return print;
		}

		private void Add(object? token)
		{
			Tokens.Add(token);
			_hash.Add(token);
		}

		private static object Box(int value) => value >= 0 && value < 64 ? Small[value] : value;

		private void Decline(string reason)
		{
			Unsupported = true;
			Reason ??= reason;
		}

		public override Expression? Visit(Expression? node)
		{
			if (node is null)
			{
				Add(null);

				return null;
			}

			if (Foreign(node.NodeType))
			{
				Decline("node-kind");

				return node;
			}

			Add(NodeTypes[(int)node.NodeType]);
			Add(node.Type);

			return base.Visit(node);
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			if (IsSource(node.Value, _entryType))
				Add(SourceToken);
			else if (node.Value is IQueryable)
				Decline("other-queryable");
			else
			{
				Add(ArgumentToken);
				Arguments.Add(node.Value);
			}

			return node;
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			Add(node.Method);

			return base.VisitMethodCall(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			foreach (var parameter in node.Parameters)
				_parameters.TryAdd(parameter, _parameters.Count);

			Add(Box(node.Parameters.Count));

			return base.VisitLambda(node);
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			if (_parameters.TryGetValue(node, out var ordinal))
				Add(Box(ordinal));
			else
				Decline("unbound-parameter");

			return node;
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			Add(node.Member);

			return base.VisitMember(node);
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			Add(node.Method);

			return base.VisitUnary(node);
		}

		protected override Expression VisitBinary(BinaryExpression node)
		{
			Add(node.Method);
			Add(node.IsLiftedToNull ? True : False);

			return base.VisitBinary(node);
		}

		protected override Expression VisitTypeBinary(TypeBinaryExpression node)
		{
			Add(node.TypeOperand);

			return base.VisitTypeBinary(node);
		}

		protected override Expression VisitNew(NewExpression node)
		{
			Add(node.Constructor);

			if (node.Members is { } members)
			{
				foreach (var member in members)
					Add(member);
			}

			return base.VisitNew(node);
		}

		protected override MemberBinding VisitMemberBinding(MemberBinding node)
		{
			Add(BindingTypes[(int)node.BindingType]);
			Add(node.Member);

			return base.VisitMemberBinding(node);
		}

		protected override ElementInit VisitElementInit(ElementInit node)
		{
			Add(node.AddMethod);

			return base.VisitElementInit(node);
		}

		protected override Expression VisitIndex(IndexExpression node)
		{
			Add(node.Indexer);

			return base.VisitIndex(node);
		}

		protected override Expression VisitConditional(ConditionalExpression node)
		{
			Add(ConditionalToken);

			return base.VisitConditional(node);
		}
	}

	/// <summary>
	/// The walk made once per shape: replaces the cache's queryable with the <c>source</c> parameter and every other
	/// constant with an element of the <c>args</c> parameter, and rewrites <see cref="Queryable"/> calls to
	/// <see cref="Enumerable"/>.
	/// </summary>
	private sealed class Rewriter(Type entryType) : ExpressionVisitor
	{
		public ParameterExpression Source { get; } = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(entryType), "source");
		public ParameterExpression Args { get; } = Expression.Parameter(typeof(object[]), "args");
		public List<object?> Arguments { get; } = [];
		public bool Unsupported { get; private set; }
		public string? Reason { get; private set; }

		private void Decline(string reason)
		{
			Unsupported = true;
			Reason ??= reason;
		}

		public override Expression? Visit(Expression? node)
		{
			if (node is not null && Foreign(node.NodeType))
			{
				Decline("node-kind");

				return node;
			}

			return base.Visit(node);
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			if (IsSource(node.Value, entryType))
				return Source;

			if (node.Value is IQueryable)
			{
				Decline("other-queryable");

				return node;
			}

			Arguments.Add(node.Value);

			return Expression.Convert(Expression.ArrayIndex(Args, Expression.Constant(Arguments.Count - 1)), node.Type);
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			if (node.Method.DeclaringType != typeof(Queryable))
				return base.VisitMethodCall(node);

			var enumerable = EnumerableMethod(node.Method);

			if (enumerable is null)
			{
				Decline("unmapped-method");

				return node;
			}

			var arguments = new Expression[node.Arguments.Count];

			for (var i = 0; i < arguments.Length; i++)
			{
				var argument = node.Arguments[i];

				while (argument.NodeType == ExpressionType.Quote)
					argument = ((UnaryExpression)argument).Operand;

				arguments[i] = Visit(argument)!;
			}

			return Expression.Call(enumerable, arguments);
		}
	}

	/// <summary>
	/// What makes two queries the same shape: the tokens a <see cref="Fingerprint"/> wrote.
	/// </summary>
	/// <remarks>
	/// A stored key owns a copy of the tokens. A probe, one per thread, reads the walk's own list, so looking a shape up
	/// allocates nothing.
	/// </remarks>
	private sealed class ShapeKey : IEquatable<ShapeKey>
	{
		[ThreadStatic]
		private static ShapeKey? _probe;

		private object?[] _tokens = [];
		private List<object?>? _walk;
		private int _hash;

		public static ShapeKey Stored(List<object?> tokens, int hash) => new() { _tokens = [.. tokens], _hash = hash };

		public static ShapeKey Probe(List<object?> tokens, int hash)
		{
			var probe = _probe ??= new ShapeKey();

			probe._walk = tokens;
			probe._hash = hash;

			return probe;
		}

		private ReadOnlySpan<object?> Span => _walk is null ? _tokens : System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_walk);

		public bool Equals(ShapeKey? other)
		{
			if (other is null || other._hash != _hash)
				return false;

			var mine = Span;
			var theirs = other.Span;

			if (mine.Length != theirs.Length)
				return false;

			for (var i = 0; i < mine.Length; i++)
			{
				if (!ReferenceEquals(mine[i], theirs[i]) && !Equals(mine[i], theirs[i]))
					return false;
			}

			return true;
		}

		public override bool Equals(object? obj) => obj is ShapeKey other && Equals(other);

		public override int GetHashCode() => _hash;
	}
}
