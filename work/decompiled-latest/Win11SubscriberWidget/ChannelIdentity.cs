using System;

namespace Win11SubscriberWidget;

internal static class ChannelIdentity
{
	public static bool IsBilibili(string platform)
	{
		string text = (platform ?? "").Trim().ToLowerInvariant();
		return text == "bilibili" || text == "bili" || text == "b站";
	}

	public static string NormalizePlatform(string platform)
	{
		if (IsBilibili(platform))
		{
			return "bilibili";
		}
		string text = (platform ?? "").Trim().ToLowerInvariant();
		return (text == "youtube" || text == "yt" || text == "油管") ? "youtube" : text;
	}

	public static string CacheKey(ChannelConfig channel)
	{
		if (channel == null)
		{
			return "";
		}
		if (IsBilibili(channel.platform))
		{
			return First(channel.bilibili_uid, channel.uid, channel.vmid);
		}
		return First(channel.youtube_channel_id, channel.channel_id, channel.youtube_channel, channel.youtube_handle, channel.handle, channel.youtube_username, channel.username, channel.youtube_url, channel.url);
	}

	public static string ConfiguredKey(ChannelConfig channel)
	{
		if (channel == null)
		{
			return "";
		}
		if (IsBilibili(channel.platform))
		{
			return First(channel.bilibili_uid, channel.uid, channel.vmid);
		}
		return First(channel.youtube_channel, channel.youtube_handle, channel.handle, channel.youtube_username, channel.username, channel.youtube_url, channel.url, channel.youtube_channel_id, channel.channel_id);
	}

	public static CachedCountConfig FindCachedCount(WidgetConfig config, ChannelConfig channel)
	{
		if (config == null || config.cached_counts == null || channel == null)
		{
			return null;
		}
		string key = CacheKey(channel);
		string platform = NormalizePlatform(channel.platform);
		foreach (CachedCountConfig item in config.cached_counts)
		{
			if (item != null && string.Equals(item.key, key, StringComparison.OrdinalIgnoreCase) && string.Equals(item.platform, platform, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		return null;
	}

	public static void ApplyResolvedYouTubeId(WidgetConfig config, ChannelConfig channel, string channelId)
	{
		if (config == null || channel == null || string.IsNullOrWhiteSpace(channelId) || IsBilibili(channel.platform))
		{
			return;
		}
		string oldKey = CacheKey(channel);
		channel.youtube_channel_id = channelId.Trim();
		string newKey = CacheKey(channel);
		if (oldKey.Length == 0 || string.Equals(oldKey, newKey, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		MigrateKey(config, NormalizePlatform(channel.platform), oldKey, newKey);
	}

	private static void MigrateKey(WidgetConfig config, string platform, string oldKey, string newKey)
	{
		foreach (CachedCountConfig item in config.cached_counts)
		{
			Migrate(item?.platform, item?.key, platform, oldKey, delegate { item.key = newKey; });
		}
		foreach (DailyBaselineConfig item2 in config.daily_baselines)
		{
			Migrate(item2?.platform, item2?.key, platform, oldKey, delegate { item2.key = newKey; });
		}
		foreach (DailyBaselineConfig item3 in config.daily_history)
		{
			Migrate(item3?.platform, item3?.key, platform, oldKey, delegate { item3.key = newKey; });
		}
		foreach (DailyBaselineConfig item4 in config.weekly_baselines)
		{
			Migrate(item4?.platform, item4?.key, platform, oldKey, delegate { item4.key = newKey; });
		}
		foreach (AchievedMilestoneConfig item5 in config.achieved_milestones)
		{
			Migrate(item5?.platform, item5?.key, platform, oldKey, delegate { item5.key = newKey; });
		}
		foreach (WarnRecordConfig item6 in config.warn_records)
		{
			if (item6 == null || !string.Equals(item6.platform, platform, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (string.Equals(item6.key, oldKey, StringComparison.OrdinalIgnoreCase))
			{
				item6.key = newKey;
			}
			else if (string.Equals(item6.key, oldKey + ":overtaken", StringComparison.OrdinalIgnoreCase))
			{
				item6.key = newKey + ":overtaken";
			}
		}
		foreach (AchievementRecordConfig item7 in config.achievement_records)
		{
			Migrate(item7?.platform, item7?.key, platform, oldKey, delegate { item7.key = newKey; });
		}
		foreach (MonthlyUpdateConfig item8 in config.monthly_updates)
		{
			if (item8 != null && string.Equals(item8.channel_key, oldKey, StringComparison.OrdinalIgnoreCase))
			{
				item8.channel_key = newKey;
			}
		}
	}

	private static void Migrate(string itemPlatform, string itemKey, string platform, string oldKey, Action action)
	{
		if (string.Equals(itemPlatform, platform, StringComparison.OrdinalIgnoreCase) && string.Equals(itemKey, oldKey, StringComparison.OrdinalIgnoreCase))
		{
			action();
		}
	}

	public static string First(params string[] values)
	{
		foreach (string value in values)
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return "";
	}
}
