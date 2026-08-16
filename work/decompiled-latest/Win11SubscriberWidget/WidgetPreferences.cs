using System;

namespace Win11SubscriberWidget;

internal static class WidgetWindowModes
{
	public const string Free = "free";

	public const string Topmost = "topmost";

	public const string LockedTopmost = "locked_topmost";

	public static string Normalize(string mode)
	{
		if (string.Equals(mode, LockedTopmost, StringComparison.OrdinalIgnoreCase))
		{
			return LockedTopmost;
		}
		if (string.Equals(mode, Topmost, StringComparison.OrdinalIgnoreCase))
		{
			return Topmost;
		}
		return Free;
	}

	public static bool IsTopmost(string mode)
	{
		string normalized = Normalize(mode);
		return normalized == Topmost || normalized == LockedTopmost;
	}

	public static bool IsLocked(string mode)
	{
		return Normalize(mode) == LockedTopmost;
	}

	public static string Next(string mode)
	{
		switch (Normalize(mode))
		{
		case Free:
			return Topmost;
		case Topmost:
			return LockedTopmost;
		default:
			return Free;
		}
	}

	public static string DisplayName(string mode)
	{
		switch (Normalize(mode))
		{
		case Topmost:
			return "窗口置顶";
		case LockedTopmost:
			return "锁定且置顶";
		default:
			return "自由移动";
		}
	}
}

internal static class WidgetCloseActions
{
	public const string Tray = "tray";

	public const string Exit = "exit";
}
