using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Win11SubscriberWidget;

internal static class AppLogger
{
	private const long MaxLogBytes = 1048576L;

	private static readonly object SyncRoot = new object();

	public static string LogPath => Path.Combine(ConfigStore.AppDir, "widget_debug.log");

	public static void Error(string area, Exception exception)
	{
		if (exception == null)
		{
			return;
		}
		Write("ERROR", area, exception.GetType().Name + ": " + exception.Message);
	}

	public static void Info(string area, string message)
	{
		Write("INFO", area, message);
	}

	private static void Write(string level, string area, string message)
	{
		try
		{
			lock (SyncRoot)
			{
				RotateIfNeeded();
				string text = Redact(message ?? "");
				File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + level + "] [" + (area ?? "app") + "] " + text + Environment.NewLine, Encoding.UTF8);
			}
		}
		catch
		{
		}
	}

	private static void RotateIfNeeded()
	{
		if (!File.Exists(LogPath) || new FileInfo(LogPath).Length < MaxLogBytes)
		{
			return;
		}
		string text = LogPath + ".1";
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		File.Move(LogPath, text);
	}

	private static string Redact(string message)
	{
		return Regex.Replace(message, "([?&](?:key|api_key)=)[^&\\s]+", "$1***", RegexOptions.IgnoreCase);
	}
}
