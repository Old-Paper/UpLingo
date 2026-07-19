using System;
using System.Collections.Generic;

namespace Win11SubscriberWidget;

internal sealed class UsageCaptureResult
{
	public bool Changed;

	public bool NewlyCheckedIn;

	public int EarnedMakeupCards;
}

internal static class UsageStatsService
{
	public static UsageCaptureResult ApplyCapture(WidgetConfig config, IEnumerable<string> runningIds, TimeSpan elapsed, DateTime localNow)
	{
		return ApplyCapture(config, runningIds, runningIds, elapsed, localNow);
	}

	public static UsageCaptureResult ApplyCapture(WidgetConfig config, IEnumerable<string> runningIds, IEnumerable<string> activeIds, TimeSpan elapsed, DateTime localNow)
	{
		UsageCaptureResult usageCaptureResult = new UsageCaptureResult();
		if (config == null || elapsed.TotalSeconds < 1.0)
		{
			return usageCaptureResult;
		}
		config.ApplyDefaults();
		HashSet<string> hashSet = new HashSet<string>(runningIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet2 = new HashSet<string>(activeIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		if (hashSet.Count == 0 && hashSet2.Count == 0)
		{
			return usageCaptureResult;
		}
		ProfessionalCheckinCaptureResult openedResult = (hashSet.Count > 0) ? ProfessionalCheckinService.RecordOpened(config, localNow) : new ProfessionalCheckinCaptureResult();
		usageCaptureResult.Changed = openedResult.Changed;
		usageCaptureResult.NewlyCheckedIn = openedResult.NewlyCheckedIn;
		long num = Math.Max(1L, (long)Math.Round(elapsed.TotalSeconds));
		string text = localNow.ToString("yyyy-MM-dd");
		bool flag = false;
		foreach (string item in hashSet2)
		{
			ProfessionalAppDefinition professionalAppDefinition = ProfessionalAppCatalog.Find(item);
			if (professionalAppDefinition == null)
			{
				continue;
			}
			UsageStatConfig usageStatConfig = FindOrCreate(config, professionalAppDefinition);
			if (!string.Equals(usageStatConfig.today_date, text, StringComparison.Ordinal))
			{
				usageStatConfig.today_date = text;
				usageStatConfig.today_seconds = 0L;
			}
			usageStatConfig.total_seconds += num;
			usageStatConfig.today_seconds += num;
			usageStatConfig.last_used_at = localNow.ToString("o");
			flag = true;
		}
		if (!flag)
		{
			return usageCaptureResult;
		}
		ProfessionalCheckinCaptureResult professionalCheckinCaptureResult = ProfessionalCheckinService.RecordActiveTime(config, elapsed, localNow);
		usageCaptureResult.Changed = true;
		usageCaptureResult.NewlyCheckedIn = usageCaptureResult.NewlyCheckedIn || professionalCheckinCaptureResult.NewlyCheckedIn;
		usageCaptureResult.EarnedMakeupCards = professionalCheckinCaptureResult.EarnedMakeupCards;
		return usageCaptureResult;
	}

	public static List<UsageStatConfig> GetUsedRecords(WidgetConfig config)
	{
		List<UsageStatConfig> list = new List<UsageStatConfig>();
		if (config?.usage_stats == null)
		{
			return list;
		}
		foreach (UsageStatConfig usageStat in config.usage_stats)
		{
			if (usageStat != null && usageStat.total_seconds > 0L)
			{
				list.Add(usageStat);
			}
		}
		list.Sort((UsageStatConfig a, UsageStatConfig b) => b.total_seconds.CompareTo(a.total_seconds));
		return list;
	}

	public static long TotalSeconds(WidgetConfig config)
	{
		long num = 0L;
		foreach (UsageStatConfig usedRecord in GetUsedRecords(config))
		{
			num += usedRecord.total_seconds;
		}
		return num;
	}

	public static long TodaySeconds(WidgetConfig config, DateTime localNow)
	{
		long num = 0L;
		string text = localNow.ToString("yyyy-MM-dd");
		foreach (UsageStatConfig usedRecord in GetUsedRecords(config))
		{
			if (string.Equals(usedRecord.today_date, text, StringComparison.Ordinal))
			{
				num += usedRecord.today_seconds;
			}
		}
		return num;
	}

	public static string FormatDuration(long seconds)
	{
		seconds = Math.Max(0L, seconds);
		long num = seconds / 3600;
		long num2 = seconds % 3600 / 60;
		if (num > 0)
		{
			return num + " 小时 " + num2 + " 分";
		}
		if (num2 > 0)
		{
			return num2 + " 分钟";
		}
		return seconds + " 秒";
	}

	private static UsageStatConfig FindOrCreate(WidgetConfig config, ProfessionalAppDefinition app)
	{
		foreach (UsageStatConfig usageStat in config.usage_stats)
		{
			if (usageStat != null && string.Equals(usageStat.app_id, app.Id, StringComparison.OrdinalIgnoreCase))
			{
				usageStat.label = app.Label;
				return usageStat;
			}
		}
		UsageStatConfig usageStatConfig = new UsageStatConfig
		{
			app_id = app.Id,
			label = app.Label
		};
		config.usage_stats.Add(usageStatConfig);
		return usageStatConfig;
	}
}
