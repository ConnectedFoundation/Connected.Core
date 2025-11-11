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
	private readonly List<MemberMapping> _members;
	public EntityMapping(Type entityType)
	{
		EntityType = entityType;
		_members = [];

		InitializeSchema();
		InitializeMembers();
	}

	public string Id => $"{Schema}.{Name}";
	public string Name { get; private set; } = default!;
	public string Schema { get; private set; } = default!;
	private Type EntityType { get; }
	public IImmutableList<MemberMapping> Members => [.. _members];

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

	private static Expression CreateMemberExpression(ExpressionCompilationContext context, Expression root, MemberMapping member)
	{
		if (root is AliasedExpression aliasedRoot)
		{
			return new ColumnExpression(member.MemberInfo.GetMemberType() ?? throw new NullReferenceException(SR.ErrCannotResolveMemberType), context.Language.TypeSystem.ResolveColumnType(member.Type),
				 aliasedRoot.Alias, member.Name);
		}

		return Binder.Bind(root, member.MemberInfo);
	}

	private Expression CreateEntityExpression(IList<EntityAssignment> assignments)
	{
		var cons = EntityType.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic && !c.IsStatic).ToArray();
		var hasNoArgConstructor = cons.Any(c => c.GetParameters().Length == 0);
		var newExpression = Expression.New(EntityType);

		Expression result;

		if (assignments.Any())
		{
			if (EntityType.GetTypeInfo().IsInterface)
				assignments = [.. RemapAssignments(assignments)];

			result = Expression.MemberInit(newExpression, [.. assignments.Select(a => Expression.Bind(a.Mapping.MemberInfo, a.Expression))]);
		}
		else
			result = newExpression;

		return result;
	}

	private IEnumerable<EntityAssignment> RemapAssignments(IEnumerable<EntityAssignment> assignments)
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
