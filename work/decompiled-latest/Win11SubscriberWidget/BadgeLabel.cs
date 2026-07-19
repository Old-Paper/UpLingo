using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class BadgeLabel : Label
{
	public Color BadgeColor { get; set; }

	public BadgeLabel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		using GraphicsPath path = Theme.RoundedRect(new Rectangle(0, 0, base.Width - 1, base.Height - 1), 7);
		using SolidBrush brush = new SolidBrush(BadgeColor);
		e.Graphics.FillPath(brush, path);
		using StringFormat stringFormat = new StringFormat();
		stringFormat.Alignment = StringAlignment.Center;
		stringFormat.LineAlignment = StringAlignment.Center;
		using SolidBrush brush2 = new SolidBrush(ForeColor);
		e.Graphics.DrawString(Text, Font, brush2, new RectangleF(0f, 0f, base.Width, base.Height), stringFormat);
	}
}
