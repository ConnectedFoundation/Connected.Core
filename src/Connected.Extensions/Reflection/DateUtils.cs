namespace Connected.Reflection;
public static class DateUtils
{
	public static DateTime FromUtc(this DateTime value, TimeZoneInfo? timeZone)
	{
		if (value == DateTime.MinValue || value.Kind != DateTimeKind.Utc)
			return value;

		if (timeZone is null || timeZone == TimeZoneInfo.Utc)
			return value;
		else
			return TimeZoneInfo.ConvertTimeFromUtc(value, timeZone);
	}

	public static DateTime ToUtc(this DateTime value, TimeZoneInfo? timeZone)
	{
		if (value == DateTime.MinValue || value.Kind == DateTimeKind.Utc)
			return value;

		if (timeZone is null || timeZone == TimeZoneInfo.Utc)
			return value;
		else
			return TimeZoneInfo.ConvertTimeToUtc(value, timeZone);
	}

	public static DateTimeOffset FromUtc(this DateTimeOffset value, TimeZoneInfo? timeZone)
	{
		if (value == DateTimeOffset.MinValue)
			return value;

		if (timeZone is null || timeZone == TimeZoneInfo.Utc)
			return new DateTimeOffset(new DateTime(value.UtcDateTime.Ticks, DateTimeKind.Utc));
		else
		{
			var offset = FromUtc(value.UtcDateTime, timeZone);

			return new DateTimeOffset(offset, timeZone.GetUtcOffset(value.UtcDateTime));
		}
	}

	public static DateTimeOffset ToUtc(this DateTimeOffset value, TimeZoneInfo? timeZone)
	{
		if (value == DateTimeOffset.MinValue || value.Offset == TimeSpan.Zero)
			return value;

		if (timeZone is null || timeZone == TimeZoneInfo.Utc)
			return value;
		else
			return new DateTimeOffset(value.UtcDateTime);
	}
}
