using System.Reflection;

namespace Win11SubscriberWidget;

internal static class AppInfo
{
	public const string ProductName = "UpLingo";

	public static string Version { get; } = GetVersion();

	public static string DisplayName => ProductName + " v" + Version;

	public static string ExecutableName => ProductName + "-" + Version + ".exe";

	private static string GetVersion()
	{
		System.Version version = Assembly.GetExecutingAssembly().GetName().Version;
		return version.Major + "." + version.Minor + "." + version.Build;
	}
}
