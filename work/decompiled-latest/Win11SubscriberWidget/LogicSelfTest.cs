using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Win11SubscriberWidget;

internal static class LogicSelfTest
{
	public static void Run()
	{
		StringBuilder report = new StringBuilder();
		try
		{
			TestMonthlyChecks();
			TestMilestones();
			TestBenchmarkPercent();
			TestWeeklyReportGate();
			TestUsageStats();
			TestCreatorMakeupCard();
			TestCreatorHistoryRetry();
			report.AppendLine("PASS");
		}
		catch (Exception ex)
		{
			report.AppendLine("FAIL");
			report.AppendLine(ex.Message);
		}
		File.WriteAllText(Path.Combine(ConfigStore.AppDir, "logic-test.log"), report.ToString(), Encoding.UTF8);
	}

	private static void TestMonthlyChecks()
	{
		List<MonthlyUpdateConfig> history = new List<MonthlyUpdateConfig>
		{
			new MonthlyUpdateConfig { channel_key = "UC_TEST", year = 2026, month = 5 },
			new MonthlyUpdateConfig { channel_key = "UC_TEST", year = 2026, month = 6 }
		};
		int[] states = WidgetRules.BuildMonthlyStates(history, "UC_TEST", 2026, 7, historyComplete: true);
		Assert(states[4] == 1 && states[5] == 1, "已更新月份应为绿色");
		Assert(states[0] == -1, "已确认未更新的过去月份应为红色");
		Assert(states[6] == -2, "尚未结束的当前月份应为黄色");
		Assert(states[7] == 0, "未来月份应为灰色");
		Assert(WidgetRules.CalculateMonthlyStreak(history, "UC_TEST", new DateTime(2026, 7, 11)) == 2, "连胜计数不正确");
		int[] unknownStates = WidgetRules.BuildMonthlyStates(history, "UC_TEST", 2026, 7, historyComplete: false);
		Assert(unknownStates[0] == 0, "历史未补齐时不能把未知月份误判为红色");
		history.Add(new MonthlyUpdateConfig { channel_key = "UC_TEST", year = 2026, month = 4, is_makeup = true });
		Assert(WidgetRules.BuildMonthlyStates(history, "UC_TEST", 2026, 7, historyComplete: true)[3] == -3, "投稿补签月份应为紫色状态");
	}

	private static void TestMilestones()
	{
		List<long> milestones = new List<long> { 1000L, 10000L, 100000L };
		Assert(WidgetRules.NextMilestone(milestones, 5500L) == 10000L, "下一个里程碑不正确");
		Assert(WidgetRules.PreviousMilestone(milestones, 5500L) == 1000L, "上一个里程碑不正确");
		Assert(Math.Abs(WidgetRules.MilestoneProgress(milestones, 5500L) - 0.5) < 0.0001, "里程碑进度不正确");
	}

	private static void TestBenchmarkPercent()
	{
		Assert(WidgetRules.FormatBenchmarkDeltaPercent(50, 100) == "还差 50%", "落后百分比不正确");
		Assert(WidgetRules.FormatBenchmarkDeltaPercent(300, 100) == "已超过 200%", "超过百分比不正确");
		Assert(WidgetRules.FormatBenchmarkCount(300000) == "30万", "整万对标数格式不正确");
		Assert(WidgetRules.FormatBenchmarkCount(309387) == "30.9万", "对标数应使用万单位而不是完整数字");
		Assert(WidgetRules.FormatBenchmarkCount(125000000) == "1.3亿", "对标数亿单位格式不正确");
	}

	private static void TestWeeklyReportGate()
	{
		List<ChannelConfig> channels = new List<ChannelConfig>
		{
			new ChannelConfig { platform = "bilibili" },
			new ChannelConfig { platform = "youtube" },
			new ChannelConfig { platform = "youtube", benchmark = true }
		};
		List<FetchResult> results = new List<FetchResult>
		{
			new FetchResult { Ok = true },
			new FetchResult { Ok = true },
			new FetchResult { Ok = false }
		};
		Assert(WidgetRules.CanGenerateWeeklyReport(channels, results), "对标失败不应阻止周报");
		results[1].Ok = false;
		Assert(!WidgetRules.CanGenerateWeeklyReport(channels, results), "自有平台失败时不应生成周报");
	}

	private static void TestUsageStats()
	{
		WidgetConfig openedOnlyConfig = new WidgetConfig();
		UsageCaptureResult openedOnlyResult = UsageStatsService.ApplyCapture(openedOnlyConfig, new string[1] { "obs" }, Array.Empty<string>(), TimeSpan.FromSeconds(20.0), new DateTime(2026, 7, 11, 9, 0, 0));
		Assert(openedOnlyResult.NewlyCheckedIn && ProfessionalCheckinService.IsCheckedIn(openedOnlyConfig, new DateTime(2026, 7, 11)), "后台运行的软件应完成当天打开打卡");
		Assert(UsageStatsService.TotalSeconds(openedOnlyConfig) == 0, "后台运行的软件不应累计活跃工作时长");
		UsageStatsService.ApplyCapture(openedOnlyConfig, new string[1] { "obs" }, new string[1] { "obs" }, TimeSpan.FromSeconds(20.0), new DateTime(2026, 7, 11, 9, 1, 0));
		Assert(UsageStatsService.TotalSeconds(openedOnlyConfig) == 20, "前台活跃软件应累计工作时长");
		WidgetConfig widgetConfig = new WidgetConfig
		{
			usage_stats = new List<UsageStatConfig>()
		};
		Assert(UsageStatsService.ApplyCapture(widgetConfig, new string[2] { "obs", "OBS" }, TimeSpan.FromSeconds(20.0), new DateTime(2026, 7, 11, 10, 0, 0)).Changed, "使用统计未写入");
		UsageStatConfig usageStatConfig = UsageStatsService.GetUsedRecords(widgetConfig)[0];
		Assert(usageStatConfig.total_seconds == 20 && usageStatConfig.today_seconds == 20, "使用时长累计不正确");
		UsageStatsService.ApplyCapture(widgetConfig, new string[1] { "obs" }, TimeSpan.FromSeconds(10.0), new DateTime(2026, 7, 12, 10, 0, 0));
		Assert(usageStatConfig.total_seconds == 30 && usageStatConfig.today_seconds == 10, "跨日统计重置不正确");
		Assert(UsageStatsService.FormatDuration(3660) == "1 小时 1 分", "时长格式不正确");
		ProfessionalCheckinCaptureResult professionalCheckinCaptureResult = ProfessionalCheckinService.RecordActiveTime(widgetConfig, TimeSpan.FromHours(2.0), new DateTime(2026, 7, 12, 12, 0, 0));
		Assert(professionalCheckinCaptureResult.EarnedMakeupCards == 1 && widgetConfig.professional_makeup_cards == 1, "专业补签卡奖励不正确");
		Assert(ProfessionalCheckinService.TryUseMakeupCard(widgetConfig, new DateTime(2026, 7, 10)), "专业补签卡无法使用");
		Assert(ProfessionalCheckinService.IsCheckedIn(widgetConfig, new DateTime(2026, 7, 10)), "专业补签状态未记录");
	}

	private static void TestCreatorMakeupCard()
	{
		WidgetConfig widgetConfig = new WidgetConfig
		{
			creator_makeup_cards = 1,
			monthly_updates = new List<MonthlyUpdateConfig>()
		};
		DateTime dateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
		Assert(CreatorCheckinService.TryUseMakeupCard(widgetConfig, "UC_TEST", dateTime), "投稿补签卡无法使用");
		Assert(widgetConfig.creator_makeup_cards == 0, "投稿补签卡扣除不正确");
		Assert(WidgetRules.HasMonthlyMakeup(widgetConfig.monthly_updates, "UC_TEST", dateTime.Year, dateTime.Month), "投稿补签状态未记录");
		Assert(!CreatorCheckinService.TryUseMakeupCard(widgetConfig, "UC_TEST", dateTime), "同一月份不能重复补签");
		WidgetConfig widgetConfig2 = new WidgetConfig
		{
			creator_state = new CreatorStateConfig(),
			monthly_updates = new List<MonthlyUpdateConfig>()
		};
		List<DateTime> list = new List<DateTime>
		{
			new DateTime(DateTime.Now.Year, DateTime.Now.Month, 2),
			new DateTime(DateTime.Now.Year, DateTime.Now.Month, 3)
		};
		Assert(CreatorCheckinService.RecordMonthlyUploads(widgetConfig2, "UC_TEST", list, "test") == 0, "首次建立历史基线不应补发投稿补签卡");
		Assert(widgetConfig2.creator_state.makeup_rewards_armed && widgetConfig2.creator_makeup_cards == 0, "投稿补签奖励基线状态不正确");
		list.Add(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 4));
		Assert(CreatorCheckinService.RecordMonthlyUploads(widgetConfig2, "UC_TEST", list, "test") == 1 && widgetConfig2.creator_makeup_cards == 1, "新发现的额外投稿应奖励一张补签卡");
		list.Add(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 5));
		Assert(CreatorCheckinService.RecordMonthlyUploads(widgetConfig2, "UC_TEST", list, "history-retry", rewardNewUploads: false) == 0 && widgetConfig2.creator_makeup_cards == 1, "历史重试不得补发投稿补签卡");
	}

	private static void TestCreatorHistoryRetry()
	{
		DateTime now = new DateTime(2026, 7, 12, 12, 0, 0);
		CreatorStateConfig state = new CreatorStateConfig
		{
			channel_key = "UC_TEST",
			monthly_history_year = 2026,
			monthly_history_complete = false,
			monthly_history_retry_after = now.AddDays(1.0).ToString("o")
		};
		Assert(!RefreshService.NeedsFullHistory(state, "UC_TEST", now), "历史抓取退避期间不应重复消耗 API");
		Assert(RefreshService.NeedsFullHistory(state, "UC_TEST", now.AddDays(2.0)), "历史抓取退避结束后应允许重试");
		state.monthly_history_complete = true;
		Assert(!RefreshService.NeedsFullHistory(state, "UC_TEST", now.AddYears(0)), "完整历史不应重复抓取");
		Assert(RefreshService.NeedsFullHistory(state, "UC_OTHER", now), "切换频道后应重新抓取历史");
	}

	private static void Assert(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
