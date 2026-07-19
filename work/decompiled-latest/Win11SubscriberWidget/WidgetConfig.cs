using System.Collections.Generic;

namespace Win11SubscriberWidget;

public class WidgetConfig
{
	public int refresh_minutes { get; set; }

	public bool low_power_mode { get; set; }

	public bool dock_to_tray { get; set; }

	public bool show_full_counts { get; set; }

	public bool show_tray_counts { get; set; }

	public bool silent_start { get; set; }

	public bool always_on_top { get; set; }

	public bool lock_position { get; set; }

	public PositionConfig position { get; set; }

	public string youtube_api_key { get; set; }

	public List<ChannelConfig> channels { get; set; }

	public List<CachedCountConfig> cached_counts { get; set; }

	public List<DailyBaselineConfig> daily_baselines { get; set; }

	public List<DailyBaselineConfig> daily_history { get; set; }

	public List<DailyBaselineConfig> weekly_baselines { get; set; }

	public CreatorStateConfig creator_state { get; set; }

	public int overtake_warn_percent { get; set; }

	public int surge_alert_percent { get; set; }

	public List<long> milestones { get; set; }

	public List<AchievedMilestoneConfig> achieved_milestones { get; set; }

	public List<AchievementRecordConfig> achievement_records { get; set; }

	public List<MonthlyUpdateConfig> monthly_updates { get; set; }

	public List<UsageStatConfig> usage_stats { get; set; }

	public List<ProfessionalCheckinConfig> professional_checkins { get; set; }

	public int professional_makeup_cards { get; set; }

	public int creator_makeup_cards { get; set; }

	public List<WarnRecordConfig> warn_records { get; set; }

	public void ApplyDefaults()
	{
		if (refresh_minutes <= 0)
		{
			refresh_minutes = 60;
		}
		if (position == null)
		{
			position = new PositionConfig
			{
				x = 80,
				y = 80
			};
		}
		if (channels == null)
		{
			channels = new List<ChannelConfig>();
		}
		if (cached_counts == null)
		{
			cached_counts = new List<CachedCountConfig>();
		}
		if (daily_baselines == null)
		{
			daily_baselines = new List<DailyBaselineConfig>();
		}
		if (daily_history == null)
		{
			daily_history = new List<DailyBaselineConfig>();
		}
		if (weekly_baselines == null)
		{
			weekly_baselines = new List<DailyBaselineConfig>();
		}
		if (warn_records == null)
		{
			warn_records = new List<WarnRecordConfig>();
		}
		if (achieved_milestones == null)
		{
			achieved_milestones = new List<AchievedMilestoneConfig>();
		}
		if (achievement_records == null)
		{
			achievement_records = new List<AchievementRecordConfig>();
		}
		if (monthly_updates == null)
		{
			monthly_updates = new List<MonthlyUpdateConfig>();
		}
		if (usage_stats == null)
		{
			usage_stats = new List<UsageStatConfig>();
		}
		if (professional_checkins == null)
		{
			professional_checkins = new List<ProfessionalCheckinConfig>();
		}
		if (overtake_warn_percent <= 0)
		{
			overtake_warn_percent = 10;
		}
		if (surge_alert_percent <= 0)
		{
			surge_alert_percent = 10;
		}
		if (milestones == null || milestones.Count == 0)
		{
			milestones = new List<long> { 1000L, 10000L, 100000L, 1000000L, 10000000L };
		}
		if (string.IsNullOrEmpty(youtube_api_key))
		{
			youtube_api_key = "YOUR_YOUTUBE_API_KEY";
		}
		if (!ConfigStore.ConfigFileExists && channels.Count == 0)
		{
			channels.Add(new ChannelConfig
			{
				platform = "bilibili",
				label = "B站频道",
				bilibili_uid = "YOUR_BILIBILI_UID"
			});
			channels.Add(new ChannelConfig
			{
				platform = "youtube",
				label = "YouTube频道",
				youtube_channel = "@your_handle_or_UC_channel_id"
			});
		}
	}
}
