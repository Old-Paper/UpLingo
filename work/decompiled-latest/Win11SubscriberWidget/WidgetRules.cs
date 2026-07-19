using System;
using System.Collections.Generic;

namespace Win11SubscriberWidget;

internal static class WidgetRules
{
	public static int[] BuildMonthlyStates(List<MonthlyUpdateConfig> history, string channelKey, int year, int currentMonth, bool historyComplete)
	{
		int[] array = new int[12];
		for (int month = 1; month <= 12; month++)
		{
			if (month > currentMonth)
			{
				array[month - 1] = 0;
			}
			else if (HasMonthlyUpdate(history, channelKey, year, month))
			{
				array[month - 1] = 1;
			}
			else if (HasMonthlyMakeup(history, channelKey, year, month))
			{
				array[month - 1] = -3;
			}
			else if (month == currentMonth)
			{
				array[month - 1] = -2;
			}
			else
			{
				array[month - 1] = historyComplete ? -1 : 0;
			}
		}
		return array;
	}

	public static int CalculateMonthlyStreak(List<MonthlyUpdateConfig> history, string channelKey, DateTime now)
	{
		DateTime dateTime = new DateTime(now.Year, now.Month, 1);
		if (!HasMonthlyCheckin(history, channelKey, dateTime.Year, dateTime.Month))
		{
			dateTime = dateTime.AddMonths(-1);
		}
		int num = 0;
		for (int i = 0; i < 120; i++)
		{
			if (!HasMonthlyCheckin(history, channelKey, dateTime.Year, dateTime.Month))
			{
				break;
			}
			num++;
			dateTime = dateTime.AddMonths(-1);
		}
		return num;
	}

	public static bool HasMonthlyUpdate(List<MonthlyUpdateConfig> history, string channelKey, int year, int month)
	{
		if (history == null || string.IsNullOrEmpty(channelKey))
		{
			return false;
		}
		foreach (MonthlyUpdateConfig item in history)
		{
			if (item != null && !item.is_makeup && item.year == year && item.month == month && string.Equals(item.channel_key, channelKey, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasMonthlyCheckin(List<MonthlyUpdateConfig> history, string channelKey, int year, int month)
	{
		return FindMonthlyRecord(history, channelKey, year, month) != null;
	}

	public static bool HasMonthlyMakeup(List<MonthlyUpdateConfig> history, string channelKey, int year, int month)
	{
		MonthlyUpdateConfig monthlyUpdateConfig = FindMonthlyRecord(history, channelKey, year, month);
		return monthlyUpdateConfig != null && monthlyUpdateConfig.is_makeup;
	}

	public static MonthlyUpdateConfig FindMonthlyRecord(List<MonthlyUpdateConfig> history, string channelKey, int year, int month)
	{
		if (history == null || string.IsNullOrEmpty(channelKey))
		{
			return null;
		}
		foreach (MonthlyUpdateConfig item in history)
		{
			if (item != null && item.year == year && item.month == month && string.Equals(item.channel_key, channelKey, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		return null;
	}

	public static long NextMilestone(List<long> milestones, long currentCount)
	{
		long num = 0L;
		if (milestones == null)
		{
			return num;
		}
		foreach (long milestone in milestones)
		{
			if (milestone > currentCount && (num == 0L || milestone < num))
			{
				num = milestone;
			}
		}
		return num;
	}

	public static long PreviousMilestone(List<long> milestones, long currentCount)
	{
		long num = 0L;
		if (milestones == null)
		{
			return num;
		}
		foreach (long milestone in milestones)
		{
			if (milestone > 0 && milestone <= currentCount && milestone > num)
			{
				num = milestone;
			}
		}
		return num;
	}

	public static double MilestoneProgress(List<long> milestones, long currentCount)
	{
		long next = NextMilestone(milestones, currentCount);
		if (next <= 0)
		{
			return 1.0;
		}
		long previous = PreviousMilestone(milestones, currentCount);
		long range = next - previous;
		if (range <= 0)
		{
			return 0.0;
		}
		return Math.Max(0.0, Math.Min(1.0, (double)(currentCount - previous) / range));
	}

	public static string FormatBenchmarkDeltaPercent(long mine, long benchmark)
	{
		if (benchmark <= 0)
		{
			return "暂无比例";
		}
		if (mine < benchmark)
		{
			return "还差 " + ((double)(benchmark - mine) * 100.0 / benchmark).ToString("0.#") + "%";
		}
		if (mine > benchmark)
		{
			return "已超过 " + ((double)(mine - benchmark) * 100.0 / benchmark).ToString("0.#") + "%";
		}
		return "已持平";
	}

	public static string FormatBenchmarkCount(long value)
	{
		double num = Math.Abs((double)value);
		string text = (value < 0) ? "-" : "";
		if (num >= 100000000.0)
		{
			return text + (num / 100000000.0).ToString("0.#") + "亿";
		}
		if (num >= 10000.0)
		{
			return text + (num / 10000.0).ToString("0.#") + "万";
		}
		return value.ToString("N0");
	}

	public static bool CanGenerateWeeklyReport(List<ChannelConfig> channels, List<FetchResult> results)
	{
		if (channels == null || results == null)
		{
			return false;
		}
		bool hasOwner = false;
		for (int i = 0; i < channels.Count; i++)
		{
			ChannelConfig channel = channels[i];
			if (channel == null || channel.benchmark)
			{
				continue;
			}
			hasOwner = true;
			if (i >= results.Count || results[i] == null || !results[i].Ok)
			{
				return false;
			}
		}
		return hasOwner;
	}
}
