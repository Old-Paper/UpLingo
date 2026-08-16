using System;
using System.Drawing;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal static class AppIcon
{
	public static Icon Load()
	{
		try
		{
			using Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
			if (icon != null)
			{
				return (Icon)icon.Clone();
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("app-icon", ex);
		}
		return (Icon)SystemIcons.Application.Clone();
	}
}
