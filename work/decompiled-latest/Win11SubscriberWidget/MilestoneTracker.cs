using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Win11SubscriberWidget;

internal static class MilestoneTracker
{
	public static string EventLogPath => Path.Combine(ConfigStore.AppDir, "subscriber_events.log");

	public static List<string> CheckChannel(string channelLabel, bool isBenchmark, bool hasOldCount, long oldCount, long newCount, List<long> milestones, HashSet<long> achievedMilestones, int surgeAlertPercent)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		if (!isBenchmark && milestones != null)
		{
			foreach (long milestone in milestones)
			{
				if (milestone > 0 && newCount >= milestone && (achievedMilestones == null || !achievedMilestones.Contains(milestone)))
				{
					list.Add("恭喜！" + channelLabel + " 已经达成 " + milestone.ToString("N0") + " 粉丝数！");
					list2.Add("[里程碑] " + channelLabel + " 达成 " + milestone.ToString("N0") + "（实际 " + newCount.ToString("N0") + "）");
				}
			}
		}
		if (hasOldCount && newCount != oldCount && oldCount > 0 && surgeAlertPercent > 0)
		{
			double num = (double)(newCount - oldCount) / (double)oldCount;
			if (Math.Abs(num) * 100.0 >= (double)surgeAlertPercent)
			{
				string text = ((num > 0.0) ? "+" : "");
				string text2 = channelLabel + " 粉丝数异动：" + oldCount.ToString("N0") + " → " + newCount.ToString("N0") + "（" + text + (num * 100.0).ToString("0.#") + "%）";
				if (!isBenchmark)
				{
					list.Add(text2);
				}
				list2.Add("[异动] " + text2);
			}
		}
		if (list2.Count > 0)
		{
			AppendLog(list2);
		}
		return list;
	}

	public static bool WasMilestoneLogged(string channelLabel, long milestone)
	{
		try
		{
			if (!File.Exists(EventLogPath))
			{
				return false;
			}
			string value = "[里程碑] " + channelLabel + " 达成 " + milestone.ToString("N0") + "（";
			string[] array = File.ReadAllLines(EventLogPath, Encoding.UTF8);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].IndexOf(value, StringComparison.Ordinal) >= 0)
				{
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("milestone-read", ex);
		}
		return false;
	}

	public static void LogWarn(string line)
	{
		AppendLog(new List<string> { "[预警] " + line });
	}

	public static void LogAchievement(string line)
	{
		AppendLog(new List<string> { "[里程碑] " + line });
	}

	public static void LogReport(string line)
	{
		AppendLog(new List<string> { "[周报] " + line });
	}

	private static void AppendLog(List<string> lines)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			foreach (string line in lines)
			{
				stringBuilder.AppendLine(text + " " + line);
			}
			File.AppendAllText(EventLogPath, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			AppLogger.Error("milestone-write", ex);
		}
	}
}
