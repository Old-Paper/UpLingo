using System.Drawing;
using System.Drawing.Drawing2D;

namespace Win11SubscriberWidget;

internal static class Theme
{
	public static readonly Color WindowBackground = Color.FromArgb(16, 21, 31);

	public static readonly Color PanelBackground = Color.FromArgb(18, 25, 38);

	public static readonly Color CardBackground = Color.FromArgb(24, 33, 52);

	public static readonly Color CardBorder = Color.FromArgb(38, 51, 76);

	public static readonly Color WindowBorder = Color.FromArgb(45, 58, 82);

	public static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);

	public static readonly Color TextSecondary = Color.FromArgb(203, 213, 225);

	public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

	public static readonly Color Success = Color.FromArgb(134, 239, 172);

	public static readonly Color Warning = Color.FromArgb(252, 211, 77);

	public static readonly Color Error = Color.FromArgb(252, 165, 165);

	public static readonly Color BiliAccent = Color.FromArgb(0, 165, 224);

	public static readonly Color YouTubeAccent = Color.FromArgb(255, 0, 51);

	public static readonly Color BenchmarkGold = Color.FromArgb(250, 204, 21);

	public static readonly Color BenchmarkTray = Color.FromArgb(202, 138, 4);

	public static readonly Color TrackBackground = Color.FromArgb(37, 48, 66);

	public static readonly Color ProgressAhead = Color.FromArgb(74, 222, 128);

	public static readonly Color InputBackground = Color.FromArgb(24, 33, 52);

	public static readonly Color CloseHoverBackground = Color.FromArgb(190, 45, 60);

	public const string UiFontFamily = "Microsoft YaHei UI";

	public const string NumberFontFamily = "Segoe UI";

	public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		if (radius <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
		{
			graphicsPath.AddRectangle(bounds);
			return graphicsPath;
		}
		int num = radius * 2;
		if (num > bounds.Width)
		{
			num = bounds.Width;
		}
		if (num > bounds.Height)
		{
			num = bounds.Height;
		}
		Rectangle rect = new Rectangle(bounds.X, bounds.Y, num, num);
		graphicsPath.AddArc(rect, 180f, 90f);
		rect.X = bounds.Right - num;
		graphicsPath.AddArc(rect, 270f, 90f);
		rect.Y = bounds.Bottom - num;
		graphicsPath.AddArc(rect, 0f, 90f);
		rect.X = bounds.X;
		graphicsPath.AddArc(rect, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}
}
