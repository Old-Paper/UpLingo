using System;
using System.IO;
using System.Text;

namespace Win11SubscriberWidget;

internal static class WeeklyReportStore
{
	private const string FormatMarker = "[format:2]";

	private static readonly string Separator = new string('-', 48);

	public static string ReportPath => Path.Combine(ConfigStore.AppDir, "weekly_report.txt");

	public static bool HasReportForWeek(DateTime weekMonday)
	{
		try
		{
			if (!File.Exists(ReportPath))
			{
				return false;
			}
			string text = File.ReadAllText(ReportPath, Encoding.UTF8);
			string text2 = "[week:" + weekMonday.ToString("yyyy-MM-dd") + "]";
			int num = text.IndexOf(text2, StringComparison.Ordinal);
			if (num < 0)
			{
				return false;
			}
			int num2 = text.IndexOf("[week:", num + text2.Length, StringComparison.Ordinal);
			return ((num2 < 0) ? text.Substring(num) : text.Substring(num, num2 - num)).IndexOf("[format:2]", StringComparison.Ordinal) >= 0;
		}
		catch (Exception ex)
		{
			AppLogger.Error("weekly-report-read", ex);
			return false;
		}
	}

	public static void Append(DateTime weekMonday, string report)
	{
		if (string.IsNullOrEmpty(report))
		{
			return;
		}
		try
		{
		DateTime dateTime = weekMonday.AddDays(-7.0);
		DateTime dateTime2 = weekMonday.AddDays(-1.0);
		StringBuilder stringBuilder = new StringBuilder();
		string value = "[week:" + weekMonday.ToString("yyyy-MM-dd") + "]";
		stringBuilder.AppendLine(value);
		stringBuilder.AppendLine("[format:2]");
		stringBuilder.AppendLine("统计周期：" + dateTime.ToString("yyyy-MM-dd") + " 至 " + dateTime2.ToString("yyyy-MM-dd"));
		stringBuilder.AppendLine("生成时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		stringBuilder.AppendLine(report);
		stringBuilder.AppendLine(Separator);
		string text = stringBuilder.ToString();
		string text2 = (File.Exists(ReportPath) ? File.ReadAllText(ReportPath, Encoding.UTF8) : "");
		int num = text2.IndexOf(value, StringComparison.Ordinal);
		if (num >= 0)
		{
			int num2 = text2.IndexOf(Separator, num, StringComparison.Ordinal);
			if (num2 >= 0)
			{
				for (num2 += Separator.Length; num2 < text2.Length && (text2[num2] == '\r' || text2[num2] == '\n'); num2++)
				{
				}
				text2 = text2.Substring(0, num) + text + text2.Substring(num2);
				File.WriteAllText(ReportPath, text2, Encoding.UTF8);
				return;
			}
		}
		if (text2.Length > 0 && !text2.EndsWith("\r\n") && !text2.EndsWith("\n"))
		{
			text2 += Environment.NewLine;
		}
		if (text2.Length > 0)
		{
			text2 += Environment.NewLine;
		}
		File.WriteAllText(ReportPath, text2 + text, Encoding.UTF8);
		}
		catch (Exception ex)
		{
			AppLogger.Error("weekly-report-write", ex);
		}
	}
}
