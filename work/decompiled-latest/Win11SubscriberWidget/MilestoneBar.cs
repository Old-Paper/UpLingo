using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class MilestoneBar : Control
{
	private double? ratio;

	public Color AccentColor { get; set; } = Theme.TextMuted;

	public MilestoneBar()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	public void SetRatio(double? value)
	{
		ratio = value;
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		int radius = Math.Max(1, (base.Height - 1) / 2);
		using (GraphicsPath path = Theme.RoundedRect(new Rectangle(0, 0, base.Width - 1, base.Height - 1), radius))
		{
			using SolidBrush brush = new SolidBrush(Theme.TrackBackground);
			e.Graphics.FillPath(brush, path);
		}
		if (!ratio.HasValue)
		{
			return;
		}
		double num = Math.Max(0.0, Math.Min(1.0, ratio.Value));
		int num2 = (int)Math.Round((double)(base.Width - 1) * num);
		if (num2 <= 0)
		{
			return;
		}
		using GraphicsPath path2 = Theme.RoundedRect(new Rectangle(0, 0, Math.Max(base.Height, num2), base.Height - 1), radius);
		using SolidBrush brush2 = new SolidBrush((ratio.Value >= 1.0) ? Theme.ProgressAhead : AccentColor);
		e.Graphics.FillPath(brush2, path2);
	}
}
