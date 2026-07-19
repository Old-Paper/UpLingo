using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Win11SubscriberWidget;

internal static class SloganProvider
{
	public const int MaxDisplayLength = 16;

	private static readonly string[] Defaults = new string[5] { "保持更新，增长自会发生", "稳定输出，就是最强复利", "今天也比昨天更近一步", "每个粉丝都值得被认真对待", "持续创作，答案会在路上" };

	private static readonly Random Random = new Random();

	private static string previous;

	public static string SloganPath => Path.Combine(ConfigStore.AppDir, "motivational_slogans.txt");

	public static string Next()
	{
		EnsureFile();
		List<string> list = Load();
		if (list.Count == 0)
		{
			list.AddRange(Defaults);
		}
		int num = Random.Next(list.Count);
		if (list.Count > 1 && string.Equals(list[num], previous, StringComparison.Ordinal))
		{
			num = (num + 1 + Random.Next(list.Count - 1)) % list.Count;
		}
		previous = list[num];
		return previous;
	}

	public static string ToDisplayText(string slogan)
	{
		if (string.IsNullOrEmpty(slogan) || slogan.Length <= 16)
		{
			return slogan ?? "";
		}
		return slogan.Substring(0, 15) + "…";
	}

	private static void EnsureFile()
	{
		try
		{
			if (!File.Exists(SloganPath))
			{
				File.WriteAllLines(SloganPath, Defaults, Encoding.UTF8);
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("slogan-create", ex);
		}
	}

	private static List<string> Load()
	{
		List<string> list = new List<string>();
		try
		{
			string[] array = File.ReadAllLines(SloganPath, Encoding.UTF8);
			for (int i = 0; i < array.Length; i++)
			{
				string text = (array[i] ?? "").Trim();
				if (text.Length > 0)
				{
					list.Add(text);
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("slogan-read", ex);
		}
		return list;
	}
}
