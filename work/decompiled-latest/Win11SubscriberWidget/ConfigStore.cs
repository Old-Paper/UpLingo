using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace Win11SubscriberWidget;

internal static class ConfigStore
{
	public static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;

	public static readonly string ConfigPath = Path.Combine(AppDir, "config.json");

	public static readonly string BackupPath = Path.Combine(AppDir, "config.json.bak");

	private static readonly object SaveLock = new object();

	private static long saveRequestVersion;

	private static long lastWrittenVersion;

	public static bool ConfigFileExists;

	public static WidgetConfig Load()
	{
		ConfigFileExists = File.Exists(ConfigPath);
		if (!ConfigFileExists)
		{
			WidgetConfig widgetConfig = CreateDefault();
			Save(widgetConfig);
			return widgetConfig;
		}
		if (TryLoadFile(ConfigPath, out var widgetConfig2))
		{
			widgetConfig2.ApplyDefaults();
			return widgetConfig2;
		}
		PreserveBrokenConfig();
		if (TryLoadFile(BackupPath, out var recovered))
		{
			recovered.ApplyDefaults();
			WriteAtomic(recovered, backupExisting: false);
			AppLogger.Info("config", "主配置损坏，已从备份恢复");
			return recovered;
		}
		WidgetConfig fallback = CreateDefault();
		WriteAtomic(fallback, backupExisting: false);
		AppLogger.Info("config", "配置无法读取，已重建默认配置");
		return fallback;
	}

	public static void Save(WidgetConfig config)
	{
		if (config == null)
		{
			throw new ArgumentNullException(nameof(config));
		}
		SaveVersioned(config, Interlocked.Increment(ref saveRequestVersion));
	}

	public static void SaveDeferred(WidgetConfig config)
	{
		if (config == null)
		{
			return;
		}
		WidgetConfig snapshot = Clone(config);
		long version = Interlocked.Increment(ref saveRequestVersion);
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				SaveVersioned(snapshot, version);
			}
			catch (Exception ex)
			{
				AppLogger.Error("config-save-background", ex);
			}
		});
	}

	public static WidgetConfig Clone(WidgetConfig config)
	{
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		return javaScriptSerializer.Deserialize<WidgetConfig>(javaScriptSerializer.Serialize(config));
	}

	private static WidgetConfig CreateDefault()
	{
		WidgetConfig obj = new WidgetConfig
		{
			refresh_minutes = 60,
			low_power_mode = true,
			dock_to_tray = true,
			show_full_counts = false,
			show_tray_counts = true,
			silent_start = false,
			always_on_top = false,
			position = new PositionConfig
			{
				x = 80,
				y = 80
			},
			youtube_api_key = "YOUR_YOUTUBE_API_KEY",
			channels = new List<ChannelConfig>(),
			cached_counts = new List<CachedCountConfig>()
		};
		ConfigFileExists = false;
		obj.ApplyDefaults();
		return obj;
	}

	private static bool TryLoadFile(string path, out WidgetConfig config)
	{
		config = null;
		try
		{
			if (!File.Exists(path))
			{
				return false;
			}
			string input = File.ReadAllText(path, Encoding.UTF8);
			config = new JavaScriptSerializer().Deserialize<WidgetConfig>(input);
			return config != null;
		}
		catch (Exception ex)
		{
			AppLogger.Error("config-load", ex);
			return false;
		}
	}

	private static void PreserveBrokenConfig()
	{
		try
		{
			if (File.Exists(ConfigPath))
			{
				string destFileName = Path.Combine(AppDir, "config.corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
				File.Copy(ConfigPath, destFileName, overwrite: false);
				File.Delete(ConfigPath);
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("config-preserve", ex);
		}
	}

	private static void WriteAtomic(WidgetConfig config, bool backupExisting)
	{
		config.ApplyDefaults();
		string json = PrettyJson(new JavaScriptSerializer().Serialize(config));
		string text = ConfigPath + ".tmp";
		try
		{
			File.WriteAllText(text, json, Encoding.UTF8);
			if (!File.Exists(ConfigPath))
			{
				File.Move(text, ConfigPath);
				ConfigFileExists = true;
				return;
			}
			if (backupExisting)
			{
				File.Copy(ConfigPath, BackupPath, overwrite: true);
			}
			try
			{
				File.Replace(text, ConfigPath, null, ignoreMetadataErrors: true);
			}
			catch (PlatformNotSupportedException)
			{
				File.Copy(text, ConfigPath, overwrite: true);
				File.Delete(text);
			}
			catch (IOException)
			{
				File.Copy(text, ConfigPath, overwrite: true);
				File.Delete(text);
			}
			ConfigFileExists = true;
		}
		finally
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}

	private static void SaveVersioned(WidgetConfig config, long version)
	{
		lock (SaveLock)
		{
			if (version < lastWrittenVersion)
			{
				return;
			}
			WriteAtomic(config, backupExisting: true);
			lastWrittenVersion = version;
		}
	}

	private static string PrettyJson(string json)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		bool flag2 = false;
		int num = 0;
		foreach (char c in json)
		{
			if (flag2)
			{
				stringBuilder.Append(c);
				flag2 = false;
				continue;
			}
			if (c == '\\' && flag)
			{
				stringBuilder.Append(c);
				flag2 = true;
				continue;
			}
			if (c == '"')
			{
				flag = !flag;
				stringBuilder.Append(c);
				continue;
			}
			if (flag)
			{
				stringBuilder.Append(c);
				continue;
			}
			switch (c)
			{
			case '[':
			case '{':
				stringBuilder.Append(c);
				stringBuilder.AppendLine();
				num++;
				AppendIndent(stringBuilder, num);
				break;
			case ']':
			case '}':
				stringBuilder.AppendLine();
				num--;
				AppendIndent(stringBuilder, num);
				stringBuilder.Append(c);
				break;
			case ',':
				stringBuilder.Append(c);
				stringBuilder.AppendLine();
				AppendIndent(stringBuilder, num);
				break;
			case ':':
				stringBuilder.Append(": ");
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private static void AppendIndent(StringBuilder builder, int indent)
	{
		for (int i = 0; i < indent; i++)
		{
			builder.Append("  ");
		}
	}
}
