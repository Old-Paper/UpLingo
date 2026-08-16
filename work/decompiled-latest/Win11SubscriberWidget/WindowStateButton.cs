using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class WindowStateButton : Control
{
	private string mode = WidgetWindowModes.Free;

	private bool hovered;

	public string Mode
	{
		get => mode;
		set
		{
			mode = WidgetWindowModes.Normalize(value);
			AccessibleName = WidgetWindowModes.DisplayName(mode);
			Invalidate();
		}
	}

	public WindowStateButton()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		Cursor = Cursors.Hand;
		TabStop = true;
		Size = new Size(26, 24);
	}

	protected override void OnMouseEnter(System.EventArgs e)
	{
		base.OnMouseEnter(e);
		hovered = true;
		Invalidate();
	}

	protected override void OnMouseLeave(System.EventArgs e)
	{
		base.OnMouseLeave(e);
		hovered = false;
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		if (hovered)
		{
			using SolidBrush background = new SolidBrush(Theme.CardBackground);
			e.Graphics.FillRectangle(background, ClientRectangle);
		}
		Color color = hovered ? Theme.TextPrimary : Theme.TextMuted;
		if (WidgetWindowModes.IsLocked(mode))
		{
			DrawLock(e.Graphics, color);
		}
		else
		{
			DrawPin(e.Graphics, color, WidgetWindowModes.IsTopmost(mode) ? -28f : 90f);
		}
	}

	private void DrawPin(Graphics graphics, Color color, float rotation)
	{
		GraphicsState state = graphics.Save();
		graphics.TranslateTransform(Width / 2f, Height / 2f);
		graphics.RotateTransform(rotation);
		using Pen pen = new Pen(color, 1.8f)
		{
			StartCap = LineCap.Round,
			EndCap = LineCap.Round,
			LineJoin = LineJoin.Round
		};
		using SolidBrush brush = new SolidBrush(color);
		graphics.FillRoundedRectangle(brush, new RectangleF(-5f, -7f, 10f, 4.5f), 2f);
		graphics.DrawLine(pen, -4f, -1.5f, 4f, -1.5f);
		graphics.DrawLine(pen, 0f, -2f, 0f, 6f);
		graphics.DrawLine(pen, 0f, 6f, -1.7f, 8.2f);
		graphics.Restore(state);
	}

	private void DrawLock(Graphics graphics, Color color)
	{
		using Pen pen = new Pen(color, 1.8f)
		{
			StartCap = LineCap.Round,
			EndCap = LineCap.Round
		};
		using SolidBrush brush = new SolidBrush(Color.FromArgb(70, color));
		using SolidBrush keyBrush = new SolidBrush(color);
		RectangleF body = new RectangleF(7f, 10f, 12f, 9f);
		graphics.FillRoundedRectangle(brush, body, 2f);
		graphics.DrawRoundedRectangle(pen, body, 2f);
		graphics.DrawArc(pen, 9f, 4f, 8f, 11f, 190f, 160f);
		graphics.FillEllipse(keyBrush, 12f, 13f, 2f, 3f);
	}
}

internal static class GraphicsRoundedRectangleExtensions
{
	public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
	{
		using GraphicsPath path = RoundedRectangle(bounds, radius);
		graphics.FillPath(brush, path);
	}

	public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF bounds, float radius)
	{
		using GraphicsPath path = RoundedRectangle(bounds, radius);
		graphics.DrawPath(pen, path);
	}

	private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
	{
		float diameter = radius * 2f;
		GraphicsPath path = new GraphicsPath();
		path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180f, 90f);
		path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270f, 90f);
		path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
		path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
		path.CloseFigure();
		return path;
	}
}
