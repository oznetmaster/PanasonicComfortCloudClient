// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace PanasonicComfortCloudClient.Models;

/// <summary>Creates and edits typed weekly schedules using the upstream seven-day/six-pattern rules.</summary>
public static class ScheduleEditing
	{
	/// <summary>Creates an explicitly inactive AC weekly schedule.</summary>
	/// <param name="deviceGuid">The device to which the complete schedule belongs.</param>
	/// <returns>Seven empty weekdays, Monday first, with activation false.</returns>
	public static AcSchedule CreateAc (string deviceGuid) => string.IsNullOrWhiteSpace (deviceGuid)
			? throw new ArgumentException ("A device GUID is required.", nameof (deviceGuid))
			: new ()
				{
				DeviceGuid = deviceGuid,
				Enabled = false,
				Days = [.. Enumerable.Range (0, 7).Select (day => new WeeklyTimerDay<AcTimerPattern> { Patterns = [], Weekday = (DayOfWeek)((day + 1) % 7) })]
				};
	/// <summary>Creates an explicitly inactive Aquarea weekly schedule.</summary>
	/// <returns>Seven empty weekdays, Monday first, with operation zero.</returns>
	public static AquareaSchedule CreateAquarea () => new () { Timer = new () { Operation = 0, Days = [.. Enumerable.Range (0, 7).Select (day => new WeeklyTimerDay<AquareaTimerPattern> { Patterns = [], Weekday = (DayOfWeek)((day + 1) % 7) })] } };
	/// <summary>Selects one existing AC weekday without changing any other day.</summary>
	/// <param name="schedule">The complete AC schedule.</param>
	/// <param name="weekday">The device-local weekday.</param>
	/// <returns>The selected mutable day.</returns>
	public static WeeklyTimerDay<AcTimerPattern> Day (this AcSchedule schedule, DayOfWeek weekday)
		{
		ArgumentNullException.ThrowIfNull (schedule);
		return Day (schedule.Days, weekday);
		}
	/// <summary>Selects one existing Aquarea weekday.</summary>
	/// <param name="schedule">The complete Aquarea schedule.</param>
	/// <param name="weekday">The device-local weekday.</param>
	/// <returns>The selected mutable day.</returns>
	public static WeeklyTimerDay<AquareaTimerPattern> Day (this AquareaSchedule schedule, DayOfWeek weekday)
		{
		ArgumentNullException.ThrowIfNull (schedule);
		return Day (schedule.Timer?.Days, weekday);
		}
	/// <summary>Adds a pattern, replacing an entry with the same start time and retaining other active entries.</summary>
	/// <typeparam name="TPattern">The device family's pattern type.</typeparam>
	/// <param name="day">The day to edit.</param>
	/// <param name="pattern">The intended scheduled settings.</param>
	/// <returns>The inserted pattern, after normalizing its start time.</returns>
	public static TPattern AddPattern<TPattern> (this WeeklyTimerDay<TPattern> day, TPattern pattern) where TPattern : TimerPattern
		{
		ArgumentNullException.ThrowIfNull (day);
		ArgumentNullException.ThrowIfNull (pattern);
		string time = NormalizeTime (pattern.StartTime);
		List<TPattern> retained = [.. (day.Patterns ?? []).Where (p => !string.IsNullOrEmpty (p.StartTime) && p.StartTime != time)];
		if (retained.Count >= 6)
			throw new ArgumentException ("A weekday supports at most six active patterns.", nameof (pattern));
		pattern.StartTime = time;
		retained.Add (pattern);
		day.Patterns = [.. retained.OrderBy (p => p.StartTime, StringComparer.Ordinal)];
		return pattern;
		}
	/// <summary>Removes an active pattern by normalized start time.</summary>
	/// <typeparam name="TPattern">The device family's pattern type.</typeparam>
	/// <param name="day">The day to edit.</param>
	/// <param name="startTime">The device-local time identifying the entry.</param>
	/// <returns>True if an entry was removed.</returns>
	public static bool RemovePattern<TPattern> (this WeeklyTimerDay<TPattern> day, string startTime) where TPattern : TimerPattern
		{
		ArgumentNullException.ThrowIfNull (day);
		string time = NormalizeTime (startTime);
		return day.Patterns?.RemoveAll (p => p.StartTime == time) > 0;
		}
	/// <summary>Removes a pattern by object identity.</summary>
	/// <typeparam name="TPattern">The device family's pattern type.</typeparam>
	/// <param name="day">The day to edit.</param>
	/// <param name="pattern">The exact pattern object to remove.</param>
	/// <returns>True if that object was present.</returns>
	public static bool RemovePattern<TPattern> (this WeeklyTimerDay<TPattern> day, TPattern pattern) where TPattern : TimerPattern
		{
		ArgumentNullException.ThrowIfNull (day);
		ArgumentNullException.ThrowIfNull (pattern);
		return day.Patterns?.RemoveAll (p => ReferenceEquals (p, pattern)) > 0;
		}
	/// <summary>Clears the selected day's patterns while preserving its unknown metadata.</summary>
	/// <typeparam name="TPattern">The device family's pattern type.</typeparam>
	/// <param name="day">The day to clear.</param>
	public static void Clear<TPattern> (this WeeklyTimerDay<TPattern> day) where TPattern : TimerPattern
		{
		ArgumentNullException.ThrowIfNull (day);
		day.Patterns = [];
		}
	/// <summary>Atomically replaces a day's active patterns after validating all of them.</summary>
	/// <typeparam name="TPattern">The device family's pattern type.</typeparam>
	/// <param name="day">The day to edit.</param>
	/// <param name="patterns">The replacement pattern list.</param>
	public static void SetPatterns<TPattern> (this WeeklyTimerDay<TPattern> day, IEnumerable<TPattern> patterns) where TPattern : TimerPattern
		{
		ArgumentNullException.ThrowIfNull (day);
		ArgumentNullException.ThrowIfNull (patterns);
		var replacement = new WeeklyTimerDay<TPattern> { Patterns = [] };
		foreach (TPattern pattern in patterns)
			_ = replacement.AddPattern (pattern);
		day.Patterns = replacement.Patterns;
		}
	private static WeeklyTimerDay<TPattern> Day<TPattern> (List<WeeklyTimerDay<TPattern>>? days, DayOfWeek weekday) where TPattern : TimerPattern => !Enum.IsDefined (weekday)
			? throw new ArgumentOutOfRangeException (nameof (weekday))
			: days?.Count == 7 ? days[((int)weekday + 6) % 7] : throw new ArgumentException ("The schedule must contain all seven weekdays.", nameof (days));
	private static string NormalizeTime (string? value)
		{
		string[] parts = (value ?? "").Split (':');
		return parts.Length != 2 || !int.TryParse (parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int hours) || !int.TryParse (parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int minutes) || hours is < 0 or > 23 || minutes is < 0 or > 59
			? throw new ArgumentException ("A valid device-local HH:mm start time is required.", nameof (value))
			: FormattableString.Invariant ($"{hours:00}:{minutes:00}");
		}
	}