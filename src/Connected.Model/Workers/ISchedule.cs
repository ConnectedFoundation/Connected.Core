namespace Connected.Runtime;

public enum ScheduleInterval
{
	Once = 1,
	Second = 2,
	Minute = 3,
	Hour = 4,
	Day = 5,
	Week = 6,
	Month = 7,
	Year = 8
}

public enum ScheduleDayMode
{
	EveryNDay = 1,
	EveryWeekday = 2
}

public enum ScheduleMonthMode
{
	ExactDay = 1,
	RelativeDay = 2
}

public enum ScheduleYearMode
{
	ExactDate = 1,
	RelativeDate = 2
}

public enum ScheduleEndMode
{
	NoEnd = 1,
	Occurrence = 2,
	Date = 3
}

public enum ScheduleMonthPart
{
	Monday = 1,
	Tuesday = 2,
	Wednesday = 3,
	Thursday = 4,
	Friday = 5,
	Saturday = 6,
	Sunday = 7,
	Weekday = 8,
	WeekendDay = 9,
	Day = 10,
}

public enum ScheduleCounter
{
	First = 1,
	Second = 2,
	Third = 3,
	Fourth = 4,
	Last = 5
}

[Flags]
public enum ScheduleWeekDays
{
	None = 0,
	Monday = 1,
	Tuesday = 2,
	Wednesday = 4,
	Thursday = 8,
	Friday = 16,
	Saturday = 32,
	Sunday = 64,
	All = 127
}

public interface ISchedule
{
	ScheduleInterval Interval { get; }
	int IntervalValue { get; }

	DateTimeOffset? StartTime { get; }
	DateTimeOffset? EndTime { get; }
	DateTimeOffset? StartDate { get; }
	DateTimeOffset? EndDate { get; }
	int? Limit { get; }
	int? DayOfMonth { get; }
	ScheduleDayMode? DayMode { get; }
	ScheduleMonthMode? MonthMode { get; }
	ScheduleYearMode? YearMode { get; }
	int? MonthNumber { get; }
	ScheduleEndMode? EndMode { get; }
	ScheduleCounter? IntervalCounter { get; }
	ScheduleMonthPart? MonthPart { get; }
	ScheduleWeekDays? Weekdays { get; }
	DateTimeOffset? NextRun { get; }
	int? RunCount { get; }
}
