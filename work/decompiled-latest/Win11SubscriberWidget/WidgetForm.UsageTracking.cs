using System;
using System.Threading;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal partial class WidgetForm
{
	private void OpenUsageStats()
	{
		CaptureUsageStats(forceSave: true);
		using UsageStatsForm usageStatsForm = new UsageStatsForm(config);
		usageStatsForm.ShowDialog(this);
		UpdateCreatorCard();
	}

	private void StartUsageStatsTracking()
	{
		usageStatsLastCaptureUtc = DateTime.UtcNow;
		usageStatsLastSaveUtc = usageStatsLastCaptureUtc;
		usageStatsTimer = new System.Windows.Forms.Timer { Interval = 5000 };
		usageStatsTimer.Tick += delegate { CaptureUsageStats(forceSave: false); };
		usageStatsTimer.Start();
	}

	private void StopUsageStatsTracking()
	{
		if (usageStatsTimer == null)
		{
			return;
		}
		usageStatsTimer.Stop();
		usageStatsTimer.Dispose();
		usageStatsTimer = null;
	}

	private void CaptureUsageStats(bool forceSave)
	{
		try
		{
			DateTime utcNow = DateTime.UtcNow;
			if (usageStatsLastCaptureUtc == DateTime.MinValue)
			{
				usageStatsLastCaptureUtc = utcNow;
				return;
			}
			TimeSpan elapsed = utcNow - usageStatsLastCaptureUtc;
			usageStatsLastCaptureUtc = utcNow;
			if (elapsed.TotalSeconds <= 0.0)
			{
				return;
			}
			if (elapsed.TotalSeconds > 15.0)
			{
				elapsed = TimeSpan.FromSeconds(15.0);
			}
			if (Interlocked.CompareExchange(ref usageDetectionRunning, 1, 0) != 0)
			{
				if (forceSave)
				{
					ConfigStore.Save(config);
				}
				return;
			}
			DateTime localNow = DateTime.Now;
			if (forceSave)
			{
				try
				{
					ApplyUsageCapture(ProfessionalAppCatalog.DetectActivity(), elapsed, localNow, utcNow, forceSave: true);
				}
				finally
				{
					Interlocked.Exchange(ref usageDetectionRunning, 0);
				}
				return;
			}
			ThreadPool.QueueUserWorkItem(delegate
			{
				try
				{
					ProfessionalActivitySnapshot snapshot = ProfessionalAppCatalog.DetectActivity();
					if (appQuitting || IsDisposed || !IsHandleCreated)
					{
						Interlocked.Exchange(ref usageDetectionRunning, 0);
						return;
					}
					BeginInvoke((Action)delegate
					{
						try
						{
							ApplyUsageCapture(snapshot, elapsed, localNow, utcNow, forceSave: false);
						}
						finally
						{
							Interlocked.Exchange(ref usageDetectionRunning, 0);
						}
					});
				}
				catch (Exception ex)
				{
					Interlocked.Exchange(ref usageDetectionRunning, 0);
					AppLogger.Error("usage-detection", ex);
				}
			});
		}
		catch (Exception ex)
		{
			Interlocked.Exchange(ref usageDetectionRunning, 0);
			AppLogger.Error("usage-stats", ex);
		}
	}

	private void ApplyUsageCapture(ProfessionalActivitySnapshot snapshot, TimeSpan elapsed, DateTime localNow, DateTime utcNow, bool forceSave)
	{
		try
		{
			UsageCaptureResult result = UsageStatsService.ApplyCapture(config, snapshot.RunningIds, snapshot.ActiveIds, elapsed, localNow);
			if (result.EarnedMakeupCards > 0 && trayIcon != null)
			{
				trayIcon.ShowBalloonTip(4500, AppInfo.DisplayName + " · 打卡奖励", "今日专业软件累计时长达到奖励门槛，获得 " + result.EarnedMakeupCards + " 张专业补签卡。", ToolTipIcon.Info);
			}
			bool criticalSave = forceSave || result.NewlyCheckedIn || result.EarnedMakeupCards > 0;
			if (!result.Changed || (!criticalSave && (utcNow - usageStatsLastSaveUtc).TotalMinutes < 2.0))
			{
				return;
			}
			if (criticalSave)
			{
				ConfigStore.Save(config);
			}
			else
			{
				ConfigStore.SaveDeferred(config);
			}
			usageStatsLastSaveUtc = utcNow;
		}
		catch (Exception ex)
		{
			AppLogger.Error("usage-stats", ex);
		}
	}
}
