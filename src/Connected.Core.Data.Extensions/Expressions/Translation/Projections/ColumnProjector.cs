using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;
using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.Evaluation;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Visitors;

namespace Connected.Data.Expressions.Translation.Projections;

internal enum ProjectionAffinity
{
	Client,
	Server
}

internal sealed class ColumnProjector : DatabaseVisitor
{
	private readonly QueryLanguage _language;
	private readonly Dictionary<ColumnExpression, ColumnExpression> _map;
	private readonly List<ColumnDeclaration> _columns;
	private readonly HashSet<string> _columnNames;
	private readonly HashSet<Expression> _candidates;
	private readonly HashSet<Alias> _existingAliases;
	private readonly Alias _newAlias;

	private ColumnProjector(QueryLanguage language, ProjectionAffinity affinity, Expression expression, IEnumerable<ColumnDeclaration> existingColumns, Alias newAlias, IEnumerable<Alias> existingAliases)
	{
		_language = language;
		_newAlias = newAlias;
		_existingAliases = new HashSet<Alias>(existingAliases);
		_map = new Dictionary<ColumnExpression, ColumnExpression>();

		if (existingColumns is not null)
		{
			_columns = new List<ColumnDeclaration>(existingColumns);
			_columnNames = new HashSet<string>(existingColumns.Select(c => c.Name));
		}
		else
		{
			_columns = new List<ColumnDeclaration>();
			_columnNames = new HashSet<string>();
		}

		_candidates = ExpressionNominator.Nominate(Language, affinity, expression);
	}

	private QueryLanguage Language => _language;
	private Dictionary<ColumnExpression, ColumnExpression> Map => _map;
	private List<ColumnDeclaration> Columns => _columns;
	private HashSet<string> ColumnNames => _columnNames;
	private HashSet<Expression> Candidates => _candidates;
	private HashSet<Alias> ExistingAliases => _existingAliases;
	private Alias NewAlias => _newAlias;
	private int ColumnCounter { get; set; }
	public static ProjectedColumns ProjectColumns(QueryLanguage language, ProjectionAffinity affinity, Expression expression,
		IEnumerable<ColumnDeclaration>? existingColumns, Alias newAlias, IEnumerable<Alias> existingAliases)
	{
		var projector = new ColumnProjector(language, affinity, expression, existingColumns, newAlias, existingAliases);
		var expr = projector.Visit(expression);

		return new ProjectedColumns(expr, projector.Columns.AsReadOnly());
	}

	public static ProjectedColumns ProjectColumns(QueryLanguage language, Expression expression, IEnumerable<ColumnDeclaration>? existingColumns,
		Alias newAlias, IEnumerable<Alias> existingAliases)
	{
		return ProjectColumns(language, ProjectionAffinity.Client, expression, existingColumns, newAlias, existingAliases);
	}

	public static ProjectedColumns ProjectColumns(QueryLanguage language, ProjectionAffinity affinity, Expression expression, IEnumerable<ColumnDeclaration> existingColumns,
		Alias newAlias, params Alias[] existingAliases)
	{
		return ProjectColumns(language, affinity, expression, existingColumns, newAlias, (IEnumerable<Alias>)existingAliases);
	}

	public static ProjectedColumns ProjectColumns(QueryLanguage language, Expression expression, IEnumerable<ColumnDeclaration> existingColumns, Alias newAlias, params Alias[] existingAliases)
	{
		return ProjectColumns(language, expression, existingColumns, newAlias, (IEnumerable<Alias>)existingAliases);
	}

	protected override Expression? Visit(Expression? expression)
	{
		if (Candidates.Contains(expression))
		{
			if (expression.NodeType == (ExpressionType)DatabaseExpressionType.Column)
			{
				var column = (ColumnExpression)expression;

				if (Map.TryGetValue(column, out ColumnExpression? mapped))
					return mapped;

				foreach (ColumnDeclaration existingColumn in Columns)
				{
					if (existingColumn.Expression is ColumnExpression cex && cex.Alias == column.Alias && cex.Name == column.Name)
						return new ColumnExpression(column.Type, column.QueryType, NewAlias, existingColumn.Name);
				}

				if (ExistingAliases.Contains(column.Alias))
				{
					var ordinal = Columns.Count;
					var columnName = GetUniqueColumnName(column.Name);

					Columns.Add(new ColumnDeclaration(columnName, column, column.QueryType));

					mapped = new ColumnExpression(column.Type, column.QueryType, NewAlias, columnName);

					Map.Add(column, mapped);

					ColumnNames.Add(columnName);

					return mapped;
				}

				return column;
			}
			else
			{
				var columnName = GetNextColumnName();
				var colType = Language.TypeSystem.ResolveColumnType(expression.Type);

				Columns.Add(new ColumnDeclaration(columnName, expression, colType));

				return new ColumnExpression(expression.Type, colType, NewAlias, columnName);
			}
		}
		else
			return base.Visit(expression);
	}

	private bool IsColumnNameInUse(string name) => ColumnNames.Contains(name);

	private string GetUniqueColumnName(string name)
	{
		var baseName = name;
		var suffix = 1;

		while (IsColumnNameInUse(name))
			name = baseName + suffix++;
		return name;
	}

	private string GetNextColumnName() => GetUniqueColumnName($"c{ColumnCounter++}");
}
