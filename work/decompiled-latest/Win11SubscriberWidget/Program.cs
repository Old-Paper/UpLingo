using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		Mutex mutex = null;
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			ServicePointManager.Expect100Continue = false;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			if (HasArg(args, "--logic-test"))
			{
				LogicSelfTest.Run();
				return;
			}
			bool flag = HasArg(args, "--fetch-test") || HasArg(args, "--achievements") || HasArg(args, "--self-test");
			if (!flag)
			{
				mutex = new Mutex(initiallyOwned: true, "Local\\UpLingo.SingleInstance", out var createdNew);
				if (!createdNew)
				{
					MessageBox.Show(AppInfo.DisplayName + " 已经在运行，请查看右下角托盘。", AppInfo.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			WidgetConfig widgetConfig = ConfigStore.Load();
			if (!flag)
			{
				StartupManager.MigrateLegacyRegistration();
			}
			if (HasArg(args, "--fetch-test"))
			{
				RunFetchTest(widgetConfig);
				return;
			}
			if (HasArg(args, "--achievements"))
			{
				Application.Run(new AchievementsForm());
				return;
			}
			WidgetForm form = new WidgetForm(widgetConfig, !HasArg(args, "--self-test"));
			if (HasArg(args, "--self-test"))
			{
				System.Threading.Timer timer = new System.Threading.Timer(delegate
				{
					form.BeginInvoke((Action)delegate
					{
						form.QuitFromApp();
					});
				}, null, 800, -1);
				Application.Run(form);
				timer.Dispose();
			}
			else
			{
				Application.Run(form);
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("startup", ex);
			MessageBox.Show(AppInfo.DisplayName + " 启动失败：\r\n" + ex.Message, AppInfo.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			if (mutex != null)
			{
				try
				{
					mutex.ReleaseMutex();
				}
				catch (ApplicationException)
				{
				}
				mutex.Dispose();
			}
		}
	}

	private static void RunFetchTest(WidgetConfig config)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("fetch-test " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		RefreshPayload payload = RefreshService.Fetch(ConfigStore.Clone(config), () => false);
		for (int i = 0; i < config.channels.Count && i < payload.Results.Count; i++)
		{
			ChannelConfig channelConfig = config.channels[i];
			FetchResult fetchResult = payload.Results[i];
			if (fetchResult.Ok)
			{
				stringBuilder.AppendLine(i + 1 + ". " + channelConfig.platform + " ok " + fetchResult.Count);
			}
			else
			{
				stringBuilder.AppendLine(i + 1 + ". " + channelConfig.platform + " error " + fetchResult.Error);
			}
		}
		if (payload.Creator != null && !string.IsNullOrEmpty(payload.Creator.ChannelKey))
		{
			stringBuilder.AppendLine("creator history " + (payload.Creator.VideoTimes?.Count ?? 0) + " complete-year " + payload.Creator.CompleteHistoryYear);
		}
		File.WriteAllText(Path.Combine(ConfigStore.AppDir, "fetch-test.log"), stringBuilder.ToString(), Encoding.UTF8);
	}

	private static bool HasArg(string[] args, string name)
	{
		for (int i = 0; i < args.Length; i++)
		{
			if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
