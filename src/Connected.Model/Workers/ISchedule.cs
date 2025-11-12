namespace Connected.Workers;

/// <summary>
/// Defines the time interval unit for schedule recurrence patterns.
/// </summary>
/// <remarks>
/// This enumeration specifies the frequency at which a scheduled operation should recur,
/// ranging from one-time execution to various recurring intervals including second, minute,
/// hour, day, week, month, and year. The interval value works in conjunction with other
/// schedule properties to define complex recurrence patterns.
/// </remarks>
public enum ScheduleInterval
{
	/// <summary>
	/// Indicates the operation should execute only once.
	/// </summary>
	Once = 1,

	/// <summary>
	/// Indicates the operation should recur every specified number of seconds.
	/// </summary>
	Second = 2,

	/// <summary>
	/// Indicates the operation should recur every specified number of minutes.
	/// </summary>
	Minute = 3,

	/// <summary>
	/// Indicates the operation should recur every specified number of hours.
	/// </summary>
	Hour = 4,

	/// <summary>
	/// Indicates the operation should recur every specified number of days.
	/// </summary>
	Day = 5,

	/// <summary>
	/// Indicates the operation should recur every specified number of weeks.
	/// </summary>
	Week = 6,

	/// <summary>
	/// Indicates the operation should recur every specified number of months.
	/// </summary>
	Month = 7,

	/// <summary>
	/// Indicates the operation should recur every specified number of years.
	/// </summary>
	Year = 8
}

/// <summary>
/// Defines the mode for daily schedule recurrence patterns.
/// </summary>
/// <remarks>
/// This enumeration determines whether a daily schedule recurs at fixed day intervals
/// or only on weekdays, excluding weekends. This is particularly useful for business
/// schedules that should only run during working days.
/// </remarks>
public enum ScheduleDayMode
{
	/// <summary>
	/// Indicates the operation should recur every N days, including weekends.
	/// </summary>
	EveryNDay = 1,

	/// <summary>
	/// Indicates the operation should recur only on weekdays (Monday through Friday).
	/// </summary>
	EveryWeekday = 2
}

/// <summary>
/// Defines the mode for monthly schedule recurrence patterns.
/// </summary>
/// <remarks>
/// This enumeration determines whether a monthly schedule recurs on a specific calendar
/// day (e.g., the 15th of each month) or on a relative day (e.g., the second Tuesday
/// of each month). Relative day mode provides more flexibility for business schedules
/// that need to align with relative dates within the month.
/// </remarks>
public enum ScheduleMonthMode
{
	/// <summary>
	/// Indicates the operation should recur on a specific day of the month (e.g., the 15th).
	/// </summary>
	ExactDay = 1,

	/// <summary>
	/// Indicates the operation should recur on a relative day (e.g., second Tuesday).
	/// </summary>
	RelativeDay = 2
}

/// <summary>
/// Defines the mode for yearly schedule recurrence patterns.
/// </summary>
/// <remarks>
/// This enumeration determines whether a yearly schedule recurs on a specific calendar
/// date (e.g., January 15th) or on a relative date (e.g., the second Tuesday of March).
/// This allows for both fixed annual dates and floating dates that depend on day-of-week
/// calculations.
/// </remarks>
public enum ScheduleYearMode
{
	/// <summary>
	/// Indicates the operation should recur on a specific date each year (e.g., January 15th).
	/// </summary>
	ExactDate = 1,

	/// <summary>
	/// Indicates the operation should recur on a relative date each year (e.g., second Tuesday of March).
	/// </summary>
	RelativeDate = 2
}

/// <summary>
/// Defines the termination mode for schedule recurrence patterns.
/// </summary>
/// <remarks>
/// This enumeration specifies when a recurring schedule should stop executing. A schedule
/// can continue indefinitely, terminate after a specific number of occurrences, or end
/// on a specific date. This provides flexibility for both finite and infinite schedules.
/// </remarks>
public enum ScheduleEndMode
{
	/// <summary>
	/// Indicates the schedule should continue indefinitely with no end condition.
	/// </summary>
	NoEnd = 1,

	/// <summary>
	/// Indicates the schedule should end after a specific number of occurrences.
	/// </summary>
	Occurrence = 2,

	/// <summary>
	/// Indicates the schedule should end on or after a specific date.
	/// </summary>
	Date = 3
}

/// <summary>
/// Defines the day or day type for relative schedule patterns.
/// </summary>
/// <remarks>
/// This enumeration is used in conjunction with relative monthly and yearly schedules
/// to specify which type of day the schedule should target. It includes individual
/// days of the week as well as special categories like weekdays, weekend days, and
/// any day of the month.
/// </remarks>
public enum ScheduleMonthPart
{
	/// <summary>
	/// Represents Monday.
	/// </summary>
	Monday = 1,

	/// <summary>
	/// Represents Tuesday.
	/// </summary>
	Tuesday = 2,

	/// <summary>
	/// Represents Wednesday.
	/// </summary>
	Wednesday = 3,

	/// <summary>
	/// Represents Thursday.
	/// </summary>
	Thursday = 4,

	/// <summary>
	/// Represents Friday.
	/// </summary>
	Friday = 5,

	/// <summary>
	/// Represents Saturday.
	/// </summary>
	Saturday = 6,

	/// <summary>
	/// Represents Sunday.
	/// </summary>
	Sunday = 7,

	/// <summary>
	/// Represents any weekday (Monday through Friday).
	/// </summary>
	Weekday = 8,

	/// <summary>
	/// Represents any weekend day (Saturday or Sunday).
	/// </summary>
	WeekendDay = 9,

	/// <summary>
	/// Represents any day of the month.
	/// </summary>
	Day = 10,
}

/// <summary>
/// Defines the ordinal position for relative schedule patterns.
/// </summary>
/// <remarks>
/// This enumeration is used in conjunction with ScheduleMonthPart to specify which
/// occurrence of a particular day type should be targeted (e.g., the second Tuesday,
/// the last Friday). This enables complex relative scheduling patterns.
/// </remarks>
public enum ScheduleCounter
{
	/// <summary>
	/// Represents the first occurrence of the specified day type.
	/// </summary>
	First = 1,

	/// <summary>
	/// Represents the second occurrence of the specified day type.
	/// </summary>
	Second = 2,

	/// <summary>
	/// Represents the third occurrence of the specified day type.
	/// </summary>
	Third = 3,

	/// <summary>
	/// Represents the fourth occurrence of the specified day type.
	/// </summary>
	Fourth = 4,

	/// <summary>
	/// Represents the last occurrence of the specified day type.
	/// </summary>
	Last = 5
}

/// <summary>
/// Defines flags for selecting specific days of the week in schedule patterns.
/// </summary>
/// <remarks>
/// This flags enumeration allows multiple days of the week to be selected for weekly
/// schedules by combining individual day flags using bitwise operations. The All flag
/// represents all seven days of the week combined.
/// </remarks>
[Flags]
public enum ScheduleWeekDays
{
	/// <summary>
	/// Represents no days selected.
	/// </summary>
	None = 0,

	/// <summary>
	/// Represents Monday.
	/// </summary>
	Monday = 1,

	/// <summary>
	/// Represents Tuesday.
	/// </summary>
	Tuesday = 2,

	/// <summary>
	/// Represents Wednesday.
	/// </summary>
	Wednesday = 4,

	/// <summary>
	/// Represents Thursday.
	/// </summary>
	Thursday = 8,

	/// <summary>
	/// Represents Friday.
	/// </summary>
	Friday = 16,

	/// <summary>
	/// Represents Saturday.
	/// </summary>
	Saturday = 32,

	/// <summary>
	/// Represents Sunday.
	/// </summary>
	Sunday = 64,

	/// <summary>
	/// Represents all days of the week (Monday through Sunday).
	/// </summary>
	All = 127
}

/// <summary>
/// Defines the configuration for scheduled task execution patterns.
/// </summary>
/// <remarks>
/// This interface encapsulates all properties required to define complex recurrence patterns
/// for scheduled operations. It supports various scheduling modes including one-time execution,
/// simple intervals (second, minute, hour), and complex patterns (daily, weekly, monthly, yearly)
/// with options for exact dates or relative day calculations. The schedule can be bounded by
/// start and end dates, occurrence limits, and specific time windows. This comprehensive
/// scheduling model enables scenarios ranging from simple periodic tasks to sophisticated
/// business schedules that account for weekdays, relative dates, and custom termination conditions.
/// </remarks>
public interface ISchedule
{
	/// <summary>
	/// Gets the base interval unit for the schedule recurrence pattern.
	/// </summary>
	/// <value>
	/// A <see cref="ScheduleInterval"/> value indicating the time unit for recurrence.
	/// </value>
	ScheduleInterval Interval { get; }

	/// <summary>
	/// Gets the multiplier for the interval unit.
	/// </summary>
	/// <value>
	/// An integer representing how many interval units should pass between executions.
	/// For example, with Interval = Hour and IntervalValue = 2, the schedule recurs every 2 hours.
	/// </value>
	int IntervalValue { get; }

	/// <summary>
	/// Gets the time of day when the schedule should start executing.
	/// </summary>
	/// <value>
	/// A nullable <see cref="DateTimeOffset"/> representing the start time, or null if no specific start time is set.
	/// </value>
	DateTimeOffset? StartTime { get; }

	/// <summary>
	/// Gets the time of day when the schedule should stop executing.
	/// </summary>
	/// <value>
	/// A nullable <see cref="DateTimeOffset"/> representing the end time, or null if no specific end time is set.
	/// </value>
	DateTimeOffset? EndTime { get; }

	/// <summary>
	/// Gets the date when the schedule should begin.
	/// </summary>
	/// <value>
	/// A nullable <see cref="DateTimeOffset"/> representing the start date, or null if the schedule starts immediately.
	/// </value>
	DateTimeOffset? StartDate { get; }

	/// <summary>
	/// Gets the date when the schedule should terminate.
	/// </summary>
	/// <value>
	/// A nullable <see cref="DateTimeOffset"/> representing the end date, or null if no end date is specified.
	/// </value>
	DateTimeOffset? EndDate { get; }

	/// <summary>
	/// Gets the maximum number of times the schedule should execute.
	/// </summary>
	/// <value>
	/// A nullable integer representing the execution limit, or null if unlimited executions are allowed.
	/// </value>
	int? Limit { get; }

	/// <summary>
	/// Gets the day of the month for exact date scheduling.
	/// </summary>
	/// <value>
	/// A nullable integer representing the day number (1-31), or null if not applicable.
	/// </value>
	int? DayOfMonth { get; }

	/// <summary>
	/// Gets the mode for daily schedule patterns.
	/// </summary>
	/// <value>
	/// A nullable <see cref="ScheduleDayMode"/> value, or null if not using daily intervals.
	/// </value>
	ScheduleDayMode? DayMode { get; }

	/// <summary>
	/// Gets the mode for monthly schedule patterns.
	/// </summary>
	/// <value>
	/// A nullable <see cref="ScheduleMonthMode"/> value, or null if not using monthly intervals.
	/// </value>
	ScheduleMonthMode? MonthMode { get; }

	/// <summary>
	/// Gets the mode for yearly schedule patterns.
	/// </summary>
	/// <value>
	/// A nullable <see cref="ScheduleYearMode"/> value, or null if not using yearly intervals.
	/// </value>
	ScheduleYearMode? YearMode { get; }

	/// <summary>
	/// Gets the month number for yearly schedules.
	/// </summary>
	/// <value>
	/// A nullable integer representing the month (1-12), or null if not applicable.
	/// </value>
	int? MonthNumber { get; }

	/// <summary>
	/// Gets the termination mode for the schedule.
	/// </summary>
	/// <value>
	/// A nullable <see cref="ScheduleEndMode"/> value indicating how the schedule should end, or null for default behavior.
	/// </value>
	ScheduleEndMode? EndMode { get; }

	/// <summary>
	/// Gets the ordinal position for relative schedule patterns.
	/// </summary>
	/// <value>
	/// A nullable <see cref="ScheduleCounter"/> value specifying which occurrence (first, second, third, fourth, or last), or null if not applicable.
	/// </value>
	ScheduleCounter? IntervalCounter { get; }

	/// <summary>
	/// Gets the day type for relative schedule patterns.
	/// </summary>
	/// <value>
	/// A nullable <see cref="ScheduleMonthPart"/> value specifying the day or day type, or null if not applicable.
	/// </value>
	ScheduleMonthPart? MonthPart { get; }

	/// <summary>
	/// Gets the selected days of the week for weekly schedules.
	/// </summary>
	/// <value>
	/// A nullable <see cref="ScheduleWeekDays"/> flags value representing the selected days, or null if not applicable.
	/// </value>
	ScheduleWeekDays? Weekdays { get; }

	/// <summary>
	/// Gets the calculated date and time of the next scheduled execution.
	/// </summary>
	/// <value>
	/// A nullable <see cref="DateTimeOffset"/> representing when the schedule will next execute, or null if no next execution is scheduled.
	/// </value>
	DateTimeOffset? NextRun { get; }

	/// <summary>
	/// Gets the number of times the schedule has executed.
	/// </summary>
	/// <value>
	/// A nullable integer representing the execution count, or null if not tracked.
	/// </value>
	int? RunCount { get; }
}