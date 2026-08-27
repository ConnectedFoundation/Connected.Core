using System.Reflection;
using System.Xml.Linq;

namespace Connected.Net.Rest.OpenApi.Documentation;

/// <summary>
/// Reads the C# doc comments a member was written with,
/// off the XML documentation file the compiler generates next to its assembly.
/// </summary>
internal static class XmlDocumentationProvider
{
	// One dictionary per assembly, built the first time a member from that assembly is looked up
	// and kept for the life of the process
	private static readonly Dictionary<Assembly, IReadOnlyDictionary<string, MemberDocumentation>> _cache = [];
	private static readonly object _sync = new();

	public static string? GetSummary(MemberInfo member) => Lookup(member)?.Summary;

	public static string? GetRemarks(MemberInfo member) => Lookup(member)?.Remarks;

	public static string? GetReturns(MethodInfo method) => Lookup(method)?.Returns;

	/// <summary>
	/// The description written on the &lt;param&gt; tag matching this parameter's name
	/// </summary>
	public static string? GetParameterSummary(ParameterInfo parameter)
	{
		if (parameter.Member is not MethodInfo method || parameter.Name is null)
			return null;

		return Lookup(method)?.Parameters.GetValueOrDefault(parameter.Name);
	}

	private static MemberDocumentation? Lookup(MemberInfo member)
	{
		if (MemberDocumentationId.For(member) is not string id)
			return null;

		var assembly = member is Type type ? type.Assembly : member.Module.Assembly;
		var members = GetMembers(assembly);

		return members.TryGetValue(id, out var documentation) ? documentation : null;
	}

	private static IReadOnlyDictionary<string, MemberDocumentation> GetMembers(Assembly assembly)
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

	private static IReadOnlyDictionary<string, MemberDocumentation> Load(Assembly assembly)
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
		catch
		{
			// A malformed or stale xml file
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
				.Select(f => (Name: (string?)f.Attribute("name"), Text: NormalizeText(f.Value)))
				.Where(f => f.Name is not null && f.Text is not null)
				.ToDictionary(f => f.Name!, f => f.Text!, StringComparer.Ordinal);

			return new MemberDocumentation(
				NormalizeText(member.Element("summary")?.Value),
				NormalizeText(member.Element("remarks")?.Value),
				NormalizeText(member.Element("returns")?.Value),
				parameters);
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
