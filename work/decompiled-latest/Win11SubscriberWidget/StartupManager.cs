using System.Windows.Forms;
using Microsoft.Win32;

namespace Win11SubscriberWidget;

internal static class StartupManager
{
	private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const string ValueName = "UpLingo";

	private const string LegacyValueName = "Win11SubscriberWidget";

	public static bool IsEnabled()
	{
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: false);
		if (registryKey == null)
		{
			return false;
		}
		object value = registryKey.GetValue(ValueName) ?? registryKey.GetValue(LegacyValueName);
		return value != null && value.ToString().Length > 0;
	}

	public static void SetEnabled(bool enabled)
	{
		using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
		if (enabled)
		{
			registryKey.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"");
			registryKey.DeleteValue(LegacyValueName, throwOnMissingValue: false);
		}
		else
		{
			registryKey.DeleteValue(ValueName, throwOnMissingValue: false);
			registryKey.DeleteValue(LegacyValueName, throwOnMissingValue: false);
		}
	}

	public static void MigrateLegacyRegistration()
	{
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
		object value = registryKey?.GetValue(ValueName);
		object value2 = registryKey?.GetValue(LegacyValueName);
		if (value2 == null && (value == null || string.Equals(value.ToString(), "\"" + Application.ExecutablePath + "\"", System.StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		SetEnabled(enabled: true);
	}
}
