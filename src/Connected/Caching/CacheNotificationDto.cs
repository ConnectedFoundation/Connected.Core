using System.Collections.Generic;
using System;

namespace Connected.Caching;

public class CacheNotificationDto
{
	public CacheNotificationDto(string method)
	{
		if (string.IsNullOrWhiteSpace(method))
			throw new ArgumentException(null, nameof(method));

		Method = method;
	}

	public string? Key { get; init; }
	public List<string>? Ids { get; init; }
	public string Method { get; }
}
