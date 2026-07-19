using System;
using System.Collections.Generic;

namespace Win11SubscriberWidget;

internal static class CreatorCheckinService
{
	public static int RecordMonthlyUploads(WidgetConfig config, string channelKey, List<DateTime> videoTimes, string detectedAt, bool rewardNewUploads = true)
	{
		if (config == null || string.IsNullOrEmpty(channelKey) || videoTimes == null)
		{
			return 0;
		}
		config.ApplyDefaults();
		if (config.creator_state == null)
		{
			config.creator_state = new CreatorStateConfig();
		}
		bool makeupRewardsArmed = config.creator_state.makeup_rewards_armed;
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (DateTime videoTime in videoTimes)
		{
			DateTime dateTime = videoTime.ToLocalTime();
			string text = dateTime.ToString("yyyy-MM");
			dictionary.TryGetValue(text, out var value);
			dictionary[text] = value + 1;
		}
		int num = 0;
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			string[] array = item.Key.Split('-');
			int year = Convert.ToInt32(array[0]);
			int month = Convert.ToInt32(array[1]);
			MonthlyUpdateConfig monthlyUpdateConfig = WidgetRules.FindMonthlyRecord(config.monthly_updates, channelKey, year, month);
			if (monthlyUpdateConfig == null)
			{
				monthlyUpdateConfig = new MonthlyUpdateConfig
				{
					channel_key = channelKey,
					year = year,
					month = month
				};
				config.monthly_updates.Add(monthlyUpdateConfig);
			}
			monthlyUpdateConfig.is_makeup = false;
			monthlyUpdateConfig.detected_at = detectedAt;
			monthlyUpdateConfig.video_count = Math.Max(monthlyUpdateConfig.video_count, item.Value);
			int num2 = Math.Max(0, monthlyUpdateConfig.video_count - 1);
			if (rewardNewUploads && makeupRewardsArmed && num2 > monthlyUpdateConfig.issued_makeup_cards)
			{
				num += num2 - monthlyUpdateConfig.issued_makeup_cards;
			}
			monthlyUpdateConfig.issued_makeup_cards = Math.Max(monthlyUpdateConfig.issued_makeup_cards, num2);
		}
		config.creator_state.makeup_rewards_armed = true;
		config.creator_makeup_cards += num;
		config.monthly_updates.RemoveAll((MonthlyUpdateConfig item) => item == null || item.year < DateTime.Now.Year - 5 || item.month < 1 || item.month > 12);
		return num;
	}

	public static bool TryUseMakeupCard(WidgetConfig config, string channelKey, DateTime month)
	{
		if (config == null || string.IsNullOrEmpty(channelKey) || config.creator_makeup_cards <= 0)
		{
			return false;
		}
		DateTime dateTime = new DateTime(month.Year, month.Month, 1);
		DateTime dateTime2 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
		if (dateTime >= dateTime2)
		{
			return false;
		}
		MonthlyUpdateConfig monthlyUpdateConfig = WidgetRules.FindMonthlyRecord(config.monthly_updates, channelKey, dateTime.Year, dateTime.Month);
		if (monthlyUpdateConfig != null)
		{
			return false;
		}
		config.monthly_updates.Add(new MonthlyUpdateConfig
		{
			channel_key = channelKey,
			year = dateTime.Year,
			month = dateTime.Month,
			is_makeup = true,
			detected_at = DateTime.Now.ToString("o")
		});
		config.creator_makeup_cards--;
		return true;
	}
}
