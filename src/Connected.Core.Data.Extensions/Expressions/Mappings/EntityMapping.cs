using Connected.Annotations.Entities;
using Connected.Collections;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Reflection;
using Connected.Data.Expressions.Translation;
using Connected.Data.Expressions.Translation.Projections;
using Connected.Reflection;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using Binder = Connected.Data.Expressions.Translation.Binder;

namespace Connected.Data.Expressions.Mappings;

internal sealed class EntityMapping
{
	private List<MemberMapping> _members;
	public EntityMapping(Type entityType)
	{
		EntityType = entityType;
		_members = new();

		InitializeSchema();
		InitializeMembers();
	}

	public string Id => $"{Schema}.{Name}";
	public string Name { get; private set; } = default!;
	public string Schema { get; private set; } = default!;
	private Type EntityType { get; }
	public ImmutableList<MemberMapping> Members => [.. _members];

	private void InitializeSchema()
	{
		var att = EntityType.ResolveTableAttribute();

		if (string.IsNullOrWhiteSpace(att.Name))
			Name = EntityType.Name;
		else
			Name = att.Name;

		if (string.IsNullOrWhiteSpace(att.Schema))
			Schema = SchemaAttribute.DefaultSchema;
		else
			Schema = att.Schema;
	}

	private void InitializeMembers()
	{
		var properties = Properties.GetImplementedProperties(EntityType);

		foreach (var property in properties)
		{
			var member = new MemberMapping(property);

			if (member.IsValid)
				_members.Add(member);
		}

		_members.SortByOrdinal();
	}

	public Expression CreateExpression(ExpressionCompilationContext context)
	{
		var tableAlias = Alias.New();
		var selectAlias = Alias.New();
		var table = new TableExpression(tableAlias, EntityType, Schema, Name);
		var projector = CreateEntityExpression(context, table);
		var pc = ColumnProjector.ProjectColumns(context.Language, projector, null, selectAlias, tableAlias);

		return new ProjectionExpression(new SelectExpression(selectAlias, pc.Columns, table, null), pc.Projector);
	}

	private EntityExpression CreateEntityExpression(ExpressionCompilationContext context, Expression root)
	{
		var assignments = new List<EntityAssignment>();

		foreach (var member in Members)
		{
			if (CreateMemberExpression(context, root, member) is Expression memberExpression)
				assignments.Add(new EntityAssignment(member, memberExpression));
		}

		return new EntityExpression(EntityType, CreateEntityExpression(assignments));
	}

	private Expression CreateMemberExpression(ExpressionCompilationContext context, Expression root, MemberMapping member)
	{
		if (root is AliasedExpression aliasedRoot)
		{
			return new ColumnExpression(member.MemberInfo.GetMemberType(), context.Language.TypeSystem.ResolveColumnType(member.Type),
				 aliasedRoot.Alias, member.Name);
		}

		return Binder.Bind(root, member.MemberInfo);
	}

	private Expression CreateEntityExpression(IList<EntityAssignment> assignments)
	{
		NewExpression newExpression;
		var readonlyMembers = assignments.Where(f => f.Mapping.IsReadOnly).ToArray();
		var cons = EntityType.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic && !c.IsStatic).ToArray();
		var hasNoArgConstructor = cons.Any(c => c.GetParameters().Length == 0);

		if (readonlyMembers.Any() || !hasNoArgConstructor)
		{
			var consThatApply = cons.Select(c => BindConstructor(c, readonlyMembers)).Where(cbr => cbr is not null && !cbr.Remaining.Any()).ToList();

			if (!consThatApply.Any())
				throw new InvalidOperationException($"Cannot construct type '{EntityType}' with all mapped and included members.");

			if (readonlyMembers.Length == assignments.Count)
				return consThatApply[0].Expression;

			var r = BindConstructor(consThatApply[0].Expression.Constructor, assignments);

			newExpression = r.Expression;
			assignments = r.Remaining;
		}
		else
			newExpression = Expression.New(EntityType);

		Expression result;

		if (assignments.Any())
		{
			if (EntityType.GetTypeInfo().IsInterface)
				assignments = RemapAssignments(assignments, EntityType).ToList();

			result = Expression.MemberInit(newExpression, assignments.Select(a => Expression.Bind(a.Mapping.MemberInfo, a.Expression)).ToArray());
		}
		else
			result = newExpression;

		return result;
	}

	private ConstructorBindResult BindConstructor(ConstructorInfo cons, IList<EntityAssignment> assignments)
	{
		var ps = cons.GetParameters();
		var args = new Expression[ps.Length];
		var mis = new MemberInfo[ps.Length];
		var members = new HashSet<EntityAssignment>(assignments);
		var used = new HashSet<EntityAssignment>();

		for (var i = 0; i < ps.Length; i++)
		{
			var p = ps[i];
			var assignment = members.FirstOrDefault(a => string.Equals(p.Name, a.Mapping.Name, StringComparison.OrdinalIgnoreCase) && p.ParameterType.IsAssignableFrom(a.Expression.Type));

			if (assignment is null)
				assignment = members.FirstOrDefault(a => string.Equals(p.Name, a.Mapping.Name, StringComparison.OrdinalIgnoreCase) && p.ParameterType.IsAssignableFrom(a.Expression.Type));

			if (assignment is not null)
			{
				args[i] = assignment.Expression;

				if (mis is not null)
					mis[i] = assignment.Mapping.MemberInfo;

				used.Add(assignment);
			}
			else
			{
				var mem = Members.Where(m => string.Equals(m.Name, p.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

				if (mem is not null)
				{
					args[i] = Expression.Constant(Types.GetDefault(p.ParameterType), p.ParameterType);
					mis[i] = mem.MemberInfo;
				}
				else
					return null;
			}
		}

		members.ExceptWith(used);

		return new ConstructorBindResult(Expression.New(cons, args, mis), members);
	}

	private IEnumerable<EntityAssignment> RemapAssignments(IEnumerable<EntityAssignment> assignments, Type entityType)
	{
		foreach (var assign in assignments)
		{
			var member = Members.FirstOrDefault(f => string.Equals(f.Name, assign.Mapping.Name, StringComparison.Ordinal));

			if (member is not null)
				yield return new EntityAssignment(member, assign.Expression);
			else
				yield return assign;
		}
	}
}
