using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Connected.Net.Rest.OpenApi.Documentation;

/// <summary>
/// Reads the C# doc comments a member was written with,
/// off the XML documentation file the compiler generates next to its assembly.
/// </summary>
/// <remarks>
/// Registered as a singleton: the per-assembly dictionary is built once on first touch and
/// kept for the life of the process.
/// </remarks>
internal sealed class XmlDocumentationProvider(ILogger<XmlDocumentationProvider> logger)
{
	// One dictionary per assembly, built the first time a member from that assembly is looked up
	// and kept for the life of the process
	private readonly Dictionary<Assembly, IReadOnlyDictionary<string, MemberDocumentation>> _cache = [];
	private readonly object _sync = new();

	public string? GetSummary(MemberInfo member) => Lookup(member)?.Summary;

	public string? GetRemarks(MemberInfo member) => Lookup(member)?.Remarks;

	public string? GetReturns(MethodInfo method) => Lookup(method)?.Returns;

	/// <summary>
	/// The description written on the &lt;param&gt; tag matching this parameter's name
	/// </summary>
	public string? GetParameterSummary(ParameterInfo parameter)
	{
		if (parameter.Member is not MethodInfo method || parameter.Name is null)
			return null;

		return Lookup(method)?.Parameters.GetValueOrDefault(parameter.Name);
	}

	private const BindingFlags MemberBindingFlags =
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

	private MemberDocumentation? Lookup(MemberInfo member) => Lookup(member, []);

	private MemberDocumentation? Lookup(MemberInfo member, HashSet<MemberInfo> visited)
	{
		if (!visited.Add(member))
			return null;

		var documentation = LookupDirect(member);

		if (documentation is null || documentation.Summary is null)
		{
			foreach (var ancestor in InheritanceCandidates(member))
			{
				if (Lookup(ancestor, visited) is not MemberDocumentation inherited)
					continue;

				documentation = MemberDocumentation.Merge(documentation, inherited);

				if (documentation.Summary is not null)
					break;
			}
		}

		return documentation;
	}

	private MemberDocumentation? LookupDirect(MemberInfo member)
	{
		if (MemberDocumentationId.For(member) is not string id)
			return null;

		var assembly = member is Type type ? type.Assembly : member.Module.Assembly;
		var members = GetMembers(assembly);

		return members.TryGetValue(id, out var documentation) ? documentation : null;
	}

	/// <summary>
	/// The members a <c>&lt;inheritdoc/&gt;</c> would pull documentation from, in resolution order:
	/// base type before interfaces, the overridden/base method before same-signature interface methods.
	/// </summary>
	private static IEnumerable<MemberInfo> InheritanceCandidates(MemberInfo member)
	{
		switch (member)
		{
			case Type type:
				foreach (var ancestor in Ancestors(type))
					yield return ancestor;

				break;

			case MethodInfo method when method.DeclaringType is Type declaringType:
				var baseDefinition = method.GetBaseDefinition();

				if (!ReferenceEquals(baseDefinition, method) && baseDefinition.DeclaringType != method.DeclaringType)
					yield return baseDefinition;

				foreach (var ancestor in Ancestors(declaringType))
				{
					var match = Array.Find(ancestor.GetMethods(MemberBindingFlags),
						f => f.Name == method.Name && HaveSameSignature(f, method));

					if (match is not null)
						yield return match;
				}

				break;

			case PropertyInfo property when property.DeclaringType is Type declaringType:
				foreach (var ancestor in Ancestors(declaringType))
				{
					if (ancestor.GetProperty(property.Name, MemberBindingFlags) is PropertyInfo match)
						yield return match;
				}

				break;
		}
	}

	private static IEnumerable<Type> Ancestors(Type type)
	{
		if (type.BaseType is Type baseType && baseType != typeof(object))
			yield return baseType;

		foreach (var contract in type.GetInterfaces())
			yield return contract;
	}

	private static bool HaveSameSignature(MethodInfo left, MethodInfo right)
	{
		var leftParameters = left.GetParameters();
		var rightParameters = right.GetParameters();

		if (leftParameters.Length != rightParameters.Length)
			return false;

		for (var i = 0; i < leftParameters.Length; i++)
		{
			if (leftParameters[i].ParameterType.Name != rightParameters[i].ParameterType.Name)
				return false;
		}

		return true;
	}

	private IReadOnlyDictionary<string, MemberDocumentation> GetMembers(Assembly assembly)
	{
		lock (_sync)
		{
			if (_cache.TryGetValue(assembly, out var cached))
				return cached;

			var members = Load(assembly);

			_cache[assembly] = members;

			return members;
		}
	}

	private IReadOnlyDictionary<string, MemberDocumentation> Load(Assembly assembly)
	{
		if (FindXmlPath(assembly) is not string path)
			return EmptyMembers;

		try
		{
			var document = XDocument.Load(path);
			var result = new Dictionary<string, MemberDocumentation>(StringComparer.Ordinal);

			foreach (var member in document.Descendants("member"))
			{
				if ((string?)member.Attribute("name") is not string name || name.Length == 0)
					continue;

				result[name] = MemberDocumentation.Parse(member);
			}

			return result;
		}
		catch (Exception exception) when (exception is XmlException or IOException or UnauthorizedAccessException)
		{
			// A stale or malformed .xml
			logger.LogWarning(exception, "Ignoring unreadable XML documentation file {DocumentationFile}", path);

			return EmptyMembers;
		}
	}

	private static string? FindXmlPath(Assembly assembly)
	{
		if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
			return null;

		var path = Path.ChangeExtension(assembly.Location, ".xml");

		return File.Exists(path) ? path : null;
	}

	private static readonly IReadOnlyDictionary<string, MemberDocumentation> EmptyMembers =
		new Dictionary<string, MemberDocumentation>();

	private sealed record MemberDocumentation(string? Summary, string? Remarks, string? Returns, IReadOnlyDictionary<string, string> Parameters)
	{
		public static MemberDocumentation Parse(XElement member)
		{
			var parameters = member.Elements("param")
				.Select(f => (Name: (string?)f.Attribute("name"), Text: Render(f)))
				.Where(f => f.Name is not null && f.Text is not null)
				.ToDictionary(f => f.Name!, f => f.Text!, StringComparer.Ordinal);

			return new MemberDocumentation(
				Render(member.Element("summary")),
				Render(member.Element("remarks")),
				Render(member.Element("returns")),
				parameters);
		}

		/// <summary>
		/// Field-wise combine of a more-derived member's own documentation (<paramref name="primary"/>)
		/// with what it inherits (<paramref name="fallback"/>); the derived value wins where present.
		/// </summary>
		public static MemberDocumentation Merge(MemberDocumentation? primary, MemberDocumentation fallback)
		{
			if (primary is null)
				return fallback;

			var parameters = new Dictionary<string, string>(fallback.Parameters, StringComparer.Ordinal);

			foreach (var pair in primary.Parameters)
				parameters[pair.Key] = pair.Value;

			return new MemberDocumentation(
				primary.Summary ?? fallback.Summary,
				primary.Remarks ?? fallback.Remarks,
				primary.Returns ?? fallback.Returns,
				parameters);
		}

		/// <summary>
		/// Flattens a doc-comment element to plain text. <see cref="XElement.Value"/> concatenates
		/// only descendant text, so empty inline elements - <c>&lt;see cref&gt;</c>, <c>&lt;paramref&gt;</c>,
		/// <c>&lt;typeparamref&gt;</c>, <c>&lt;see langword&gt;</c> - contribute nothing and a sentence
		/// built around them reads as broken. Substitute a readable token for each instead.
		/// </summary>
		private static string? Render(XElement? element)
		{
			if (element is null)
				return null;

			var builder = new StringBuilder();

			Append(element, builder);

			return NormalizeText(builder.ToString());
		}

		private static void Append(XElement element, StringBuilder builder)
		{
			foreach (var node in element.Nodes())
			{
				switch (node)
				{
					case XText text:
						builder.Append(text.Value);
						break;

					case XElement child:
						AppendElement(child, builder);
						break;
				}
			}
		}

		private static void AppendElement(XElement element, StringBuilder builder)
		{
			switch (element.Name.LocalName)
			{
				case "see":
				case "seealso":
					builder.Append(ReferenceLabel(element));
					break;

				case "paramref":
				case "typeparamref":
					builder.Append((string?)element.Attribute("name"));
					break;

				default:
					Append(element, builder);
					break;
			}
		}

		/// <summary>
		/// A human-readable label for a <c>&lt;see&gt;</c>/<c>&lt;seealso&gt;</c>: explicit inner text
		/// if the author gave any, otherwise the last segment of the cref (minus the member-kind
		/// prefix, a method signature and generic arity), a langword, or an href.
		/// </summary>
		private static string? ReferenceLabel(XElement element)
		{
			if ((string?)element.Attribute("langword") is string langword)
				return langword;

			if (!string.IsNullOrEmpty(element.Value))
				return element.Value;

			if ((string?)element.Attribute("cref") is string cref)
			{
				var text = cref.Length > 1 && cref[1] == ':' ? cref[2..] : cref;

				var signature = text.IndexOf('(');

				if (signature >= 0)
					text = text[..signature];

				var arity = text.IndexOf('`');

				if (arity >= 0)
					text = text[..arity];

				var lastSeparator = text.LastIndexOf('.');

				return lastSeparator >= 0 ? text[(lastSeparator + 1)..] : text;
			}

			return (string?)element.Attribute("href");
		}

		// Collapses a doc comment's original line-breaks into a single space, and trims leading/trailing whitespace.
		private static string? NormalizeText(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			var words = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

			return words.Length == 0 ? null : string.Join(' ', words);
		}
	}
}
