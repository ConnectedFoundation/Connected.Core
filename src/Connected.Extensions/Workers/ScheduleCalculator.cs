namespace Connected.Workers;
public static class ScheduleCalculator
{
	public static DateTimeOffset? Calculate(this ISchedule schedule)
	{
		var initial = schedule.NextRun;
		var now = DateTimeOffset.UtcNow;

		if (initial > now)
			return initial;

		DateTimeOffset? result;

		do
		{
			result = schedule.Calculate(initial, now);

			if (result == DateTimeOffset.MinValue)
				return result;

			initial = result;

		} while (result < now);

		return result;
	}

	public static DateTimeOffset? Calculate(this ISchedule schedule, DateTimeOffset? initial, DateTimeOffset? current)
	{
		var origin = current ?? DateTimeOffset.UtcNow;
		origin = new DateTimeOffset(origin.Year, origin.Month, origin.Day, origin.Hour, origin.Minute, origin.Second, TimeSpan.Zero);

		initial ??= origin.AddMinutes(-1);
		var value = initial.Value;

		return schedule.Interval switch
		{
			ScheduleInterval.Second => CalcNextRunSecond(schedule, value, origin),
			ScheduleInterval.Minute => CalcNextRunMinute(schedule, value, origin),
			ScheduleInterval.Day => CalcNextRunDaily(schedule, value, origin),
			ScheduleInterval.Hour => CalcNextRunHourly(schedule, value, origin),
			ScheduleInterval.Month => CalcNextRunMonthly(schedule, value, origin),
			ScheduleInterval.Once => CalcNextRunOnce(schedule, value),
			ScheduleInterval.Week => CalcNextRunWeekly(schedule, value, origin),
			ScheduleInterval.Year => CalcNextRunYearly(schedule, value, origin),
			_ => default
		};
	}

	private static DateTimeOffset? CalcNextRunOnce(ISchedule schedule, DateTimeOffset initial)
	{
		return EnsureValidDate(schedule, FixStart(schedule, initial));
	}

	private static DateTimeOffset? CalcNextRunSecond(ISchedule schedule, DateTimeOffset initial, DateTimeOffset current)
	{
		if (HasFinished(schedule, current))
			return null;

		var nextRun = initial.AddSeconds(schedule.IntervalValue);

		nextRun = FixStart(schedule, nextRun);

		if (schedule.EndTime != DateTime.MinValue && schedule.EndTime?.TimeOfDay < nextRun.TimeOfDay)
			nextRun = AlignTime(nextRun.AddDays(1), schedule.StartTime ?? DateTimeOffset.MinValue);

		return EnsureValidDate(schedule, nextRun);
	}

	private static DateTimeOffset? CalcNextRunMinute(ISchedule schedule, DateTimeOffset initial, DateTimeOffset current)
	{
		if (HasFinished(schedule, current))
			return null;

		var nextRun = initial.AddMinutes(schedule.IntervalValue);

		nextRun = FixStart(schedule, nextRun);

		if (schedule.EndTime is not null && schedule.EndTime?.TimeOfDay < nextRun.TimeOfDay)
			nextRun = AlignTime(nextRun.AddDays(1), schedule.StartTime ?? DateTimeOffset.MinValue);

		return EnsureValidDate(schedule, nextRun);
	}

	private static DateTimeOffset? CalcNextRunHourly(ISchedule schedule, DateTimeOffset initial, DateTimeOffset current)
	{
		if (HasFinished(schedule, current))
			return null;

		var nextRun = initial.AddHours(schedule.IntervalValue);

		nextRun = FixStart(schedule, nextRun);

		if (schedule.EndTime is not null && schedule.EndTime?.TimeOfDay < nextRun.TimeOfDay)
			nextRun = AlignTime(nextRun.AddDays(1), schedule.StartTime ?? DateTimeOffset.MinValue);

		return EnsureValidDate(schedule, nextRun);
	}

	private static DateTimeOffset? CalcNextRunDaily(ISchedule schedule, DateTimeOffset initial, DateTimeOffset current)
	{
		if (HasFinished(schedule, current))
			return null;

		var nextRun = initial;

		switch (schedule.DayMode)
		{
			case ScheduleDayMode.EveryNDay:
				nextRun = nextRun.AddDays(schedule.IntervalValue);
				nextRun = FixStart(schedule, nextRun);

				return EnsureValidDate(schedule, nextRun);
			case ScheduleDayMode.EveryWeekday:
				nextRun = nextRun.AddDays(1);

				for (var i = 0; i < 7; i++)
				{
					if (nextRun.DayOfWeek == DayOfWeek.Saturday || nextRun.DayOfWeek == DayOfWeek.Sunday)
						nextRun = nextRun.AddDays(1);
					else
						break;
				}

				nextRun = FixStart(schedule, nextRun);

				return EnsureValidDate(schedule, nextRun);
			default:
				return null;
		}
	}

	private static DateTimeOffset? CalcNextRunWeekly(ISchedule schedule, DateTimeOffset initial, DateTimeOffset current)
	{
		if (HasFinished(schedule, current))
			return null;

		var daysIncrement = 7 * (schedule.IntervalValue < 1 ? 1 : schedule.IntervalValue);
		var nextRun = initial;

		if (!HasWeekdayDefined(schedule))
			return nextRun.AddDays(daysIncrement);

		for (var i = 0; i < 7; i++)
		{
			if (IsWeekdayEnabled(schedule, nextRun))
				break;

			if (IsWeekCompleted(schedule, nextRun) || nextRun.DayOfWeek == DayOfWeek.Sunday)
				nextRun = FixTime(schedule, Monday(nextRun).AddDays(daysIncrement));
			else
				nextRun = nextRun.AddDays(1);
		}

		nextRun = FixStart(schedule, nextRun);

		return EnsureValidDate(schedule, nextRun);
	}

	private static DateTimeOffset? CalcNextRunMonthly(ISchedule schedule, DateTimeOffset initial, DateTimeOffset current)
	{
		if (HasFinished(schedule, current))
			return null;

		initial = FixStart(schedule, initial);

		var nextRun = FixTime(schedule, initial);
		var intervalValue = schedule.MonthNumber == 0 ? 1 : schedule.MonthNumber;

		nextRun = new DateTimeOffset(nextRun.Year, nextRun.Month, 1, nextRun.Hour, nextRun.Minute, nextRun.Second, TimeSpan.Zero);

		if (ProcessMonthPart(schedule, nextRun, schedule.MonthMode ?? ScheduleMonthMode.ExactDay) is DateTimeOffset processedNextRun)
			nextRun = processedNextRun;
		else
			return null;

		if (nextRun.Date < current.Date)
		{
			if (schedule.MonthMode == ScheduleMonthMode.ExactDay)
				nextRun = nextRun.AddMonths(intervalValue ?? 1);
			else
			{
				if (CalcNextRunMonthly(schedule, nextRun.AddMonths(intervalValue ?? 1), current) is DateTimeOffset valid)
					nextRun = valid;
				else
					return null;
			}
		}

		return nextRun;
	}

	private static DateTimeOffset? CalcNextRunYearly(ISchedule schedule, DateTimeOffset initial, DateTimeOffset current)
	{
		if (HasFinished(schedule, current))
			return null;

		initial = FixStart(schedule, initial);

		var nextRun = FixTime(schedule, initial);

		var intervalValue = schedule.IntervalValue < 1 ? 0 : schedule.IntervalValue;

		nextRun = new DateTime(nextRun.Year, 1, 1, nextRun.Hour, nextRun.Minute, nextRun.Second);

		switch (schedule.YearMode)
		{
			case ScheduleYearMode.ExactDate:
				if (EnsureValidDate(schedule, new DateTimeOffset(nextRun.Year, schedule.MonthNumber ?? 1, schedule.DayOfMonth ?? 1, nextRun.Hour, nextRun.Minute, nextRun.Second, TimeSpan.Zero)) is DateTimeOffset valid)
					nextRun = valid;
				else
					return null;

				break;
			case ScheduleYearMode.RelativeDate:
				nextRun = new DateTime(nextRun.Year, schedule.MonthNumber ?? 1, 1, nextRun.Hour, nextRun.Minute, nextRun.Second);

				if (ProcessMonthPart(schedule, nextRun, ScheduleMonthMode.RelativeDay) is DateTimeOffset processedNextRun)
					nextRun = processedNextRun;
				else
					return null;

				break;
			default:
				return null;
		}

		if (nextRun.Date < current.Date)
		{
			if (schedule.YearMode == ScheduleYearMode.ExactDate)
				nextRun = nextRun.AddYears(intervalValue);
			else
			{
				if (CalcNextRunYearly(schedule, nextRun.AddYears(intervalValue), current) is DateTimeOffset valid)
					nextRun = valid;
				else
					return null;
			}
		}

		return nextRun;
	}

	private static DateTimeOffset? ProcessMonthPart(ISchedule schedule, DateTimeOffset nextRun, ScheduleMonthMode monthMode)
	{
		switch (monthMode)
		{
			case ScheduleMonthMode.ExactDay:
				try
				{
					nextRun = new DateTimeOffset(nextRun.Year, nextRun.Month, schedule.DayOfMonth ?? 1, nextRun.Hour, nextRun.Minute, nextRun.Second, nextRun.Offset);
				}
				catch
				{
					nextRun = nextRun.AddMonths(1).AddDays(-1);
				}

				break;
			case ScheduleMonthMode.RelativeDay:
				switch (schedule.MonthPart)
				{
					case ScheduleMonthPart.Day:
						switch (schedule.IntervalCounter)
						{
							case ScheduleCounter.Fourth:
								nextRun = nextRun.AddDays(3);
								break;
							case ScheduleCounter.Last:
								nextRun = nextRun.AddMonths(1).AddDays(-1);
								break;
							case ScheduleCounter.Second:
								nextRun = nextRun.AddDays(1);
								break;
							case ScheduleCounter.Third:
								nextRun = nextRun.AddDays(2);
								break;
						}
						break;
					case ScheduleMonthPart.Friday:
						nextRun = TargetDayOfWeek(schedule.IntervalCounter, nextRun, DayOfWeek.Friday);
						break;
					case ScheduleMonthPart.Monday:
						nextRun = TargetDayOfWeek(schedule.IntervalCounter, nextRun, DayOfWeek.Monday);
						break;
					case ScheduleMonthPart.Saturday:
						nextRun = TargetDayOfWeek(schedule.IntervalCounter, nextRun, DayOfWeek.Saturday);
						break;
					case ScheduleMonthPart.Sunday:
						nextRun = TargetDayOfWeek(schedule.IntervalCounter, nextRun, DayOfWeek.Sunday);
						break;
					case ScheduleMonthPart.Thursday:
						nextRun = TargetDayOfWeek(schedule.IntervalCounter, nextRun, DayOfWeek.Thursday);
						break;
					case ScheduleMonthPart.Tuesday:
						nextRun = TargetDayOfWeek(schedule.IntervalCounter, nextRun, DayOfWeek.Tuesday);
						break;
					case ScheduleMonthPart.Wednesday:
						nextRun = TargetDayOfWeek(schedule.IntervalCounter, nextRun, DayOfWeek.Wednesday);
						break;
					case ScheduleMonthPart.Weekday:
						switch (schedule.IntervalCounter)
						{
							case ScheduleCounter.First:
								while (nextRun.DayOfWeek == DayOfWeek.Saturday || nextRun.DayOfWeek == DayOfWeek.Sunday)
									nextRun = nextRun.AddDays(1);
								break;
							case ScheduleCounter.Fourth:
								var c = 0;

								while (c != 4)
								{
									if (nextRun.DayOfWeek != DayOfWeek.Saturday && nextRun.DayOfWeek != DayOfWeek.Sunday)
										c++;

									nextRun = nextRun.AddDays(1);
								}

								break;
							case ScheduleCounter.Last:
								nextRun = nextRun.AddMonths(1).AddDays(-1);

								while (nextRun.DayOfWeek == DayOfWeek.Saturday || nextRun.DayOfWeek == DayOfWeek.Sunday)
									nextRun = nextRun.AddDays(-1);
								break;
							case ScheduleCounter.Second:
								var c1 = 0;

								while (c1 != 2)
								{
									if (nextRun.DayOfWeek != DayOfWeek.Saturday && nextRun.DayOfWeek != DayOfWeek.Sunday)
										c1++;

									nextRun = nextRun.AddDays(1);
								}
								break;
							case ScheduleCounter.Third:
								var c2 = 0;

								while (c2 != 3)
								{
									if (nextRun.DayOfWeek != DayOfWeek.Saturday && nextRun.DayOfWeek != DayOfWeek.Sunday)
										c2++;

									nextRun = nextRun.AddDays(1);
								}
								break;
						}
						break;
					case ScheduleMonthPart.WeekendDay:
						switch (schedule.IntervalCounter)
						{
							case ScheduleCounter.First:
								while (nextRun.DayOfWeek != DayOfWeek.Saturday && nextRun.DayOfWeek != DayOfWeek.Sunday)
									nextRun = nextRun.AddDays(1);
								break;
							case ScheduleCounter.Fourth:
								var c = 0;

								while (c != 4)
								{
									if (nextRun.DayOfWeek == DayOfWeek.Saturday || nextRun.DayOfWeek == DayOfWeek.Sunday)
										c++;

									nextRun = nextRun.AddDays(1);
								}

								break;
							case ScheduleCounter.Last:
								nextRun = nextRun.AddMonths(1).AddDays(-1);

								while (nextRun.DayOfWeek != DayOfWeek.Saturday && nextRun.DayOfWeek != DayOfWeek.Sunday)
									nextRun = nextRun.AddDays(-1);
								break;
							case ScheduleCounter.Second:
								var c1 = 0;

								while (c1 != 2)
								{
									if (nextRun.DayOfWeek == DayOfWeek.Saturday || nextRun.DayOfWeek == DayOfWeek.Sunday)
										c1++;

									nextRun = nextRun.AddDays(1);
								}
								break;
							case ScheduleCounter.Third:
								var c2 = 0;

								while (c2 != 3)
								{
									if (nextRun.DayOfWeek == DayOfWeek.Saturday || nextRun.DayOfWeek == DayOfWeek.Sunday)
										c2++;

									nextRun = nextRun.AddDays(1);
								}
								break;
						}
						break;
				}
				break;
		}

		return EnsureValidDate(schedule, nextRun);
	}

	private static DateTimeOffset TargetDayOfWeek(ScheduleCounter? counter, DateTimeOffset nextRun, DayOfWeek dayOfWeek)
	{
		switch (counter)
		{
			case ScheduleCounter.First:
				while (nextRun.DayOfWeek != dayOfWeek)
					nextRun = nextRun.AddDays(1);

				break;
			case ScheduleCounter.Fourth:
				var c = 0;

				while (c != 4)
				{
					if (nextRun.DayOfWeek == dayOfWeek)
						c++;

					if (c != 4)
						nextRun = nextRun.AddDays(1);
				}

				break;
			case ScheduleCounter.Last:
				nextRun = nextRun.AddMonths(1).AddDays(-1);

				while (nextRun.DayOfWeek != dayOfWeek)
					nextRun = nextRun.AddDays(-1);

				break;
			case ScheduleCounter.Second:
				var c1 = 0;

				while (c1 != 2)
				{
					if (nextRun.DayOfWeek == dayOfWeek)
						c1++;

					if (c1 != 2)
						nextRun = nextRun.AddDays(1);
				}

				break;
			case ScheduleCounter.Third:
				var c2 = 0;

				while (c2 != 3)
				{
					if (nextRun.DayOfWeek == dayOfWeek)
						c2++;

					if (c2 != 3)
						nextRun = nextRun.AddDays(1);
				}

				break;
		}

		return nextRun;
	}

	private static bool IsWeekCompleted(ISchedule configuration, DateTimeOffset? date)
	{
		var day = date?.DayOfWeek;

		return day switch
		{
			DayOfWeek.Monday => !(IsWeekdayEnabled(configuration, DayOfWeek.Tuesday) || IsWeekdayEnabled(configuration, DayOfWeek.Wednesday) || IsWeekdayEnabled(configuration, DayOfWeek.Thursday) || IsWeekdayEnabled(configuration, DayOfWeek.Friday) || IsWeekdayEnabled(configuration, DayOfWeek.Saturday) || IsWeekdayEnabled(configuration, DayOfWeek.Sunday)),
			DayOfWeek.Tuesday => !(IsWeekdayEnabled(configuration, DayOfWeek.Wednesday) || IsWeekdayEnabled(configuration, DayOfWeek.Thursday) || IsWeekdayEnabled(configuration, DayOfWeek.Friday) || IsWeekdayEnabled(configuration, DayOfWeek.Saturday) || IsWeekdayEnabled(configuration, DayOfWeek.Sunday)),
			DayOfWeek.Wednesday => !(IsWeekdayEnabled(configuration, DayOfWeek.Thursday) || IsWeekdayEnabled(configuration, DayOfWeek.Friday) || IsWeekdayEnabled(configuration, DayOfWeek.Saturday) || IsWeekdayEnabled(configuration, DayOfWeek.Sunday)),
			DayOfWeek.Thursday => !(IsWeekdayEnabled(configuration, DayOfWeek.Friday) || IsWeekdayEnabled(configuration, DayOfWeek.Saturday) || IsWeekdayEnabled(configuration, DayOfWeek.Sunday)),
			DayOfWeek.Friday => !(IsWeekdayEnabled(configuration, DayOfWeek.Saturday) || IsWeekdayEnabled(configuration, DayOfWeek.Sunday)),
			DayOfWeek.Saturday => !IsWeekdayEnabled(configuration, DayOfWeek.Sunday),
			DayOfWeek.Sunday => true,
			_ => true,
		};
	}

	private static bool IsWeekdayEnabled(ISchedule configuration, DayOfWeek current)
	{
		var value = configuration.Weekdays ?? ScheduleWeekDays.All;

		return current switch
		{
			DayOfWeek.Friday => value.HasFlag(ScheduleWeekDays.Friday),
			DayOfWeek.Monday => value.HasFlag(ScheduleWeekDays.Monday),
			DayOfWeek.Saturday => value.HasFlag(ScheduleWeekDays.Saturday),
			DayOfWeek.Sunday => value.HasFlag(ScheduleWeekDays.Sunday),
			DayOfWeek.Thursday => value.HasFlag(ScheduleWeekDays.Thursday),
			DayOfWeek.Tuesday => value.HasFlag(ScheduleWeekDays.Tuesday),
			DayOfWeek.Wednesday => value.HasFlag(ScheduleWeekDays.Wednesday),
			_ => false,
		};
	}

	private static bool IsWeekdayEnabled(ISchedule configuration, DateTimeOffset? date)
	{
		var value = configuration.Weekdays ?? ScheduleWeekDays.All;

		return date?.DayOfWeek switch
		{
			DayOfWeek.Friday => value.HasFlag(ScheduleWeekDays.Friday),
			DayOfWeek.Monday => value.HasFlag(ScheduleWeekDays.Monday),
			DayOfWeek.Saturday => value.HasFlag(ScheduleWeekDays.Saturday),
			DayOfWeek.Sunday => value.HasFlag(ScheduleWeekDays.Sunday),
			DayOfWeek.Thursday => value.HasFlag(ScheduleWeekDays.Thursday),
			DayOfWeek.Tuesday => value.HasFlag(ScheduleWeekDays.Tuesday),
			DayOfWeek.Wednesday => value.HasFlag(ScheduleWeekDays.Wednesday),
			_ => false,
		};
	}

	private static bool HasWeekdayDefined(ISchedule configuration)
	{
		return configuration.Weekdays != ScheduleWeekDays.None;
	}

	private static bool HasFinished(ISchedule configuration, DateTimeOffset? current)
	{
		if (current is null)
			return true;

		return configuration.EndMode switch
		{
			ScheduleEndMode.Date => configuration.EndDate is not null && configuration.EndDate < current,
			ScheduleEndMode.NoEnd => false,
			ScheduleEndMode.Occurrence => configuration.Limit > 0 && configuration.RunCount >= configuration.Limit,
			_ => false,
		};
	}

	private static DateTimeOffset FixTime(ISchedule configuration, DateTimeOffset value)
	{
		if (configuration.StartTime?.TimeOfDay.Ticks == 0)
			return value;

		return AlignTime(value, configuration.StartTime ?? DateTimeOffset.MinValue);
	}

	private static DateTimeOffset FixStart(ISchedule configuration, DateTimeOffset date)
	{
		var startTime = configuration.StartTime ?? DateTimeOffset.MinValue;

		if (configuration.StartDate is null || configuration.StartDate.Value.Date <= date.Date)
		{
			if (configuration.StartTime is null)
				return date;
			else
			{
				if (configuration.StartTime?.TimeOfDay > date.TimeOfDay)
					return new DateTimeOffset(date.Year, date.Month, date.Day, startTime.Hour, startTime.Minute, startTime.Second, TimeSpan.Zero);
				else
					return date;
			}
		}
		else
		{
			var startDate = configuration.StartDate ?? DateTimeOffset.UtcNow;

			if (configuration.StartTime is null)
				return new DateTimeOffset(startDate.Year, startDate.Month, startDate.Day, date.Hour, date.Minute, date.Second, TimeSpan.Zero);
			else
			{
				if (startTime.TimeOfDay > date.TimeOfDay)
					return new DateTimeOffset(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, startTime.Minute, startTime.Second, TimeSpan.Zero);
				else
					return new DateTimeOffset(startDate.Year, startDate.Month, startDate.Day, date.Hour, date.Minute, date.Second, TimeSpan.Zero);
			}
		}
	}

	private static DateTimeOffset AlignTime(DateTimeOffset date, DateTimeOffset time)
	{
		return new DateTimeOffset(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, TimeSpan.Zero);
	}

	private static DateTimeOffset? EnsureValidDate(ISchedule configuration, DateTimeOffset? value)
	{
		if (IsValidNextRun(configuration, value))
			return value;

		return null;
	}

	private static bool IsValidNextRun(ISchedule configuration, DateTimeOffset? value)
	{
		if (value is null)
			return false;

		if (configuration.EndMode == ScheduleEndMode.Date && configuration.EndDate is not null && configuration.EndDate < value)
			return false;

		return true;
	}

	private static DateTimeOffset Monday(DateTimeOffset date)
	{
		var diff = date.DayOfWeek - DayOfWeek.Monday;

		if (diff < 0)
			diff += 7;

		return date.AddDays(-1 * diff).Date;
	}
}

