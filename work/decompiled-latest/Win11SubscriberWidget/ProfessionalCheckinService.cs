using System;
using System.Collections.Generic;

namespace Win11SubscriberWidget;

internal sealed class ProfessionalCheckinCaptureResult
{
	public bool Changed;

	public bool NewlyCheckedIn;

	public int EarnedMakeupCards;
}

internal static class ProfessionalCheckinService
{
	public static ProfessionalCheckinCaptureResult RecordOpened(WidgetConfig config, DateTime localNow)
	{
		ProfessionalCheckinCaptureResult result = new ProfessionalCheckinCaptureResult();
		if (config == null)
		{
			return result;
		}
		config.ApplyDefaults();
		ProfessionalCheckinConfig record = FindOrCreate(config, localNow.Date);
		if (!record.opened)
		{
			record.opened = true;
			record.is_makeup = false;
			record.first_opened_at = localNow.ToString("o");
			result.Changed = true;
			result.NewlyCheckedIn = true;
		}
		return result;
	}

	public static ProfessionalCheckinCaptureResult RecordActiveTime(WidgetConfig config, TimeSpan elapsed, DateTime localNow)
	{
		ProfessionalCheckinCaptureResult professionalCheckinCaptureResult = new ProfessionalCheckinCaptureResult();
		if (config == null || elapsed.TotalSeconds < 1.0)
		{
			return professionalCheckinCaptureResult;
		}
		ProfessionalCheckinCaptureResult openedResult = RecordOpened(config, localNow);
		long num = Math.Max(1L, (long)Math.Round(elapsed.TotalSeconds));
		ProfessionalCheckinConfig professionalCheckinConfig = FindOrCreate(config, localNow.Date);
		professionalCheckinCaptureResult.NewlyCheckedIn = openedResult.NewlyCheckedIn;
		professionalCheckinConfig.active_seconds += num;
		int num2 = (int)(professionalCheckinConfig.active_seconds / 7200L);
		if (num2 > professionalCheckinConfig.issued_makeup_cards)
		{
			professionalCheckinCaptureResult.EarnedMakeupCards = num2 - professionalCheckinConfig.issued_makeup_cards;
			professionalCheckinConfig.issued_makeup_cards = num2;
			config.professional_makeup_cards += professionalCheckinCaptureResult.EarnedMakeupCards;
		}
		professionalCheckinCaptureResult.Changed = true;
		string text = localNow.Date.AddYears(-5).ToString("yyyy-MM-dd");
		config.professional_checkins.RemoveAll((ProfessionalCheckinConfig item) => item == null || string.CompareOrdinal(item.date, text) < 0);
		return professionalCheckinCaptureResult;
	}

	public static bool TryUseMakeupCard(WidgetConfig config, DateTime date)
	{
		if (config == null || date.Date >= DateTime.Now.Date || config.professional_makeup_cards <= 0)
		{
			return false;
		}
		ProfessionalCheckinConfig professionalCheckinConfig = FindOrCreate(config, date.Date);
		if (professionalCheckinConfig.opened || professionalCheckinConfig.is_makeup)
		{
			return false;
		}
		professionalCheckinConfig.is_makeup = true;
		config.professional_makeup_cards--;
		return true;
	}

	public static bool IsCheckedIn(WidgetConfig config, DateTime date)
	{
		ProfessionalCheckinConfig professionalCheckinConfig = Find(config, date.Date);
		return professionalCheckinConfig != null && (professionalCheckinConfig.opened || professionalCheckinConfig.is_makeup);
	}

	public static int GetStreak(WidgetConfig config, DateTime localNow)
	{
		DateTime dateTime = localNow.Date;
		if (!IsCheckedIn(config, dateTime))
		{
			dateTime = dateTime.AddDays(-1.0);
		}
		int num = 0;
		for (int i = 0; i < 4000 && IsCheckedIn(config, dateTime); i++)
		{
			num++;
			dateTime = dateTime.AddDays(-1.0);
		}
		return num;
	}

	public static List<ProfessionalCheckinConfig> GetRecent(WidgetConfig config, DateTime lastDay, int days)
	{
		List<ProfessionalCheckinConfig> list = new List<ProfessionalCheckinConfig>();
		for (int i = Math.Max(0, days - 1); i >= 0; i--)
		{
			ProfessionalCheckinConfig professionalCheckinConfig = Find(config, lastDay.Date.AddDays(-i));
			if (professionalCheckinConfig != null)
			{
				list.Add(professionalCheckinConfig);
			}
		}
		return list;
	}

	public static ProfessionalCheckinConfig Find(WidgetConfig config, DateTime date)
	{
		if (config?.professional_checkins == null)
		{
			return null;
		}
		string text = date.ToString("yyyy-MM-dd");
		foreach (ProfessionalCheckinConfig professionalCheckin in config.professional_checkins)
		{
			if (professionalCheckin != null && string.Equals(professionalCheckin.date, text, StringComparison.Ordinal))
			{
				return professionalCheckin;
			}
		}
		return null;
	}

	private static ProfessionalCheckinConfig FindOrCreate(WidgetConfig config, DateTime date)
	{
		ProfessionalCheckinConfig professionalCheckinConfig = Find(config, date);
		if (professionalCheckinConfig != null)
		{
			return professionalCheckinConfig;
		}
		professionalCheckinConfig = new ProfessionalCheckinConfig
		{
			date = date.ToString("yyyy-MM-dd")
		};
		config.professional_checkins.Add(professionalCheckinConfig);
		return professionalCheckinConfig;
	}
}
