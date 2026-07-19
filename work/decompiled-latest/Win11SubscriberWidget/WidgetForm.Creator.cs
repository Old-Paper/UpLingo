using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal partial class WidgetForm
{
	private Control CreateCreatorCard()
	{
		creatorCard = new CardPanel
		{
			ShowCheckinIndicators = true,
			BackColor = Theme.PanelBackground,
			Size = new Size(ClientSize.Width - 28, 54),
			Margin = new Padding(0, 4, 0, 4)
		};
		creatorDot = new Label
		{
			Text = "●",
			ForeColor = Theme.TextMuted,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 8f),
			Size = new Size(18, 18),
			TextAlign = ContentAlignment.MiddleCenter,
			Location = new Point(12, 8)
		};
		creatorCard.Controls.Add(creatorDot);
		creatorLabel = new Label
		{
			Text = "视频动态获取中…",
			ForeColor = Theme.TextSecondary,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 8.5f),
			AutoEllipsis = true,
			Size = new Size(creatorCard.Width - 44, 18),
			Location = new Point(34, 8)
		};
		creatorCard.Controls.Add(creatorLabel);
		creatorSubLabel = new SegmentedTextLabel
		{
			ForeColor = Theme.TextMuted,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 8f),
			AutoEllipsis = true,
			Size = new Size(creatorCard.Width - 44, 16),
			Location = new Point(34, 30)
		};
		creatorCard.Controls.Add(creatorSubLabel);
		AttachWidgetMouseEvents(creatorCard);
		AttachWidgetMouseEvents(creatorDot);
		AttachWidgetMouseEvents(creatorLabel);
		AttachWidgetMouseEvents(creatorSubLabel);
		EventHandler openStudio = delegate
		{
			try { Process.Start("https://studio.youtube.com"); }
			catch (Exception ex) { AppLogger.Error("youtube-studio-open", ex); }
		};
		creatorCard.DoubleClick += openStudio;
		creatorLabel.DoubleClick += openStudio;
		creatorSubLabel.DoubleClick += openStudio;
		rowToolTip.SetToolTip(creatorCard, "双击打开 YouTube Studio");
		rowToolTip.SetToolTip(creatorLabel, "双击打开 YouTube Studio");
		return creatorCard;
	}

	private void RestoreCreatorState()
	{
		latestVideoAt = null;
		if (config.creator_state != null && !string.IsNullOrEmpty(config.creator_state.configured_channel_key) && !string.Equals(config.creator_state.configured_channel_key, OwnerYouTubeConfiguredKey(config), StringComparison.OrdinalIgnoreCase))
		{
			config.creator_state = null;
			return;
		}
		if (config.creator_state != null && DateTime.TryParse(config.creator_state.last_video_at, out var value))
		{
			latestVideoAt = value;
		}
	}

	private void UpdateCreatorCard()
	{
		if (creatorCard == null || creatorDot == null || creatorLabel == null)
		{
			return;
		}
		string channelKey = ActiveCreatorChannelKey();
		bool historyComplete = IsMonthlyHistoryComplete(DateTime.Now.Year, channelKey);
		if (!latestVideoAt.HasValue && !historyComplete)
		{
			creatorDot.ForeColor = Theme.TextMuted;
			creatorLabel.ForeColor = Theme.TextMuted;
			creatorLabel.Text = DescribeCreatorCardPending();
			creatorSubLabel.Text = "";
			creatorCard.SetMonthChecks(null, 0, ProfessionalCheckinService.GetStreak(config, DateTime.Now));
			creatorCard.EdgeAccent = null;
			return;
		}
		bool updated = HasYouTubeVideoInMonth(DateTime.Now.Year, DateTime.Now.Month);
		string message = updated ? "本月已经更新！别懈怠！" : ((weeklyMetricMode % 2 == 0) ? (DateTime.Now.Month + "月还没更新，别逼爱兰丝动手！") : (DateTime.Now.Month + "月还没更新，我会一直盯着你！"));
		Color color = updated ? Theme.Success : Theme.Error;
		int creatorStreak = CalculateMonthlyUpdateStreak();
		int softwareStreak = ProfessionalCheckinService.GetStreak(config, DateTime.Now);
		creatorCard.SetMonthChecks(BuildMonthlyCheckStates(), creatorStreak, softwareStreak);
		rowToolTip.SetToolTip(creatorCard, "投稿打卡连胜 " + creatorStreak + " 个月（红黄火焰）\n专业软件打卡连胜 " + softwareStreak + " 天（蓝紫火焰）\n双击打开 YouTube Studio");
		creatorDot.ForeColor = color;
		creatorLabel.ForeColor = color;
		creatorLabel.Text = message;
		creatorCard.EdgeAccent = color;
		UpdateCreatorSubLabel();
	}

	private int[] BuildMonthlyCheckStates()
	{
		DateTime now = DateTime.Now;
		string channelKey = ActiveCreatorChannelKey();
		return WidgetRules.BuildMonthlyStates(config.monthly_updates, channelKey, now.Year, now.Month, IsMonthlyHistoryComplete(now.Year, channelKey));
	}

	private int CalculateMonthlyUpdateStreak() => WidgetRules.CalculateMonthlyStreak(config.monthly_updates, ActiveCreatorChannelKey(), DateTime.Now);

	private bool HasYouTubeVideoInMonth(int year, int month)
	{
		string channelKey = ActiveCreatorChannelKey();
		if (WidgetRules.HasMonthlyUpdate(config.monthly_updates, channelKey, year, month))
		{
			return true;
		}
		if (lastCreatorFetch?.VideoTimes != null)
		{
			foreach (DateTime videoTime in lastCreatorFetch.VideoTimes)
			{
				DateTime local = videoTime.ToLocalTime();
				if (local.Year == year && local.Month == month) return true;
			}
		}
		if (latestVideoAt.HasValue)
		{
			DateTime local = latestVideoAt.Value.ToLocalTime();
			if (local.Year == year && local.Month == month) return true;
		}
		return false;
	}

	private string ActiveCreatorChannelKey() => (lastCreatorFetch != null && !string.IsNullOrEmpty(lastCreatorFetch.ChannelKey)) ? lastCreatorFetch.ChannelKey : (config.creator_state?.channel_key ?? "");

	private bool IsMonthlyHistoryComplete(int year, string channelKey) => config.creator_state != null && config.creator_state.monthly_history_complete && config.creator_state.monthly_history_year == year && string.Equals(config.creator_state.channel_key, channelKey, StringComparison.OrdinalIgnoreCase);

	private string OwnerYouTubeConfiguredKey(WidgetConfig source)
	{
		if (source?.channels == null) return "";
		foreach (ChannelConfig channel in source.channels)
		{
			if (channel != null && !channel.benchmark && !IsBilibili(channel.platform)) return ChannelIdentity.ConfiguredKey(channel);
		}
		return "";
	}

	private string DescribeCreatorCardPending()
	{
		string apiKey = (config.youtube_api_key ?? "").Trim();
		if (apiKey.Length == 0 || apiKey.StartsWith("YOUR_")) return "填好 YouTube API key 后，这里会显示断更提醒";
		if (lastResults != null)
		{
			for (int i = 0; i < lastResults.Count && i < config.channels.Count; i++)
			{
				ChannelConfig channel = config.channels[i];
				if (!channel.benchmark && !IsBilibili(channel.platform) && lastResults[i] != null && !lastResults[i].Ok && !lastResults[i].HasCached) return "YouTube 频道还没读取成功，修好后这里会亮";
			}
		}
		return "等待首次刷新，稍后显示视频动态";
	}

	private void UpdateCreatorSubLabel()
	{
		if (creatorSubLabel == null) return;
		creatorSubLabel.SetSegments(Tuple.Create("本周涨粉：", Theme.TextMuted), Tuple.Create("B站 " + FormatWeeklyPlatformGain(true), Theme.BiliAccent), Tuple.Create("  ·  ", Theme.TextMuted), Tuple.Create("YT " + FormatWeeklyPlatformGain(false), Theme.YouTubeAccent));
	}

	private string FormatWeeklyPlatformGain(bool bilibili)
	{
		if (lastResults == null) return "--";
		for (int i = 0; i < lastResults.Count && i < config.channels.Count; i++)
		{
			ChannelConfig channel = config.channels[i];
			if (channel.benchmark || IsBilibili(channel.platform) != bilibili) continue;
			FetchResult result = lastResults[i];
			if (!HasEffectiveCount(result)) return "--";
			long count = EffectiveCount(result);
			DailyBaselineConfig baseline = FindWeeklyBaseline(NormalizePlatform(channel.platform), ChannelCacheKey(channel));
			return FormatSignedDelta(count - (baseline?.count ?? count));
		}
		return "--";
	}

	private string FormatSignedDelta(long value) => ((value >= 0) ? "+" : "-") + FormatDisplayCount(Math.Abs(value));
}
