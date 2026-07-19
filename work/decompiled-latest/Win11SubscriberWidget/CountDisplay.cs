using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class CountDisplay : Control
{
	private string numberPart = "--";

	private string unitPart = "";

	public CountDisplay()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	public void SetCount(string text)
	{
		text = text ?? "";
		int num;
		for (num = text.Length; num > 0; num--)
		{
			char c = text[num - 1];
			if (char.IsDigit(c) || c == ',' || c == '.' || c == '-')
			{
				break;
			}
		}
		numberPart = text.Substring(0, num);
		unitPart = text.Substring(num);
		if (numberPart.Length == 0)
		{
			numberPart = text;
			unitPart = "";
		}
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		using Font font = new Font("Segoe UI", 17f, FontStyle.Bold);
		using Font font2 = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold);
		using SolidBrush brush = new SolidBrush(ForeColor);
		SizeF sizeF = e.Graphics.MeasureString(numberPart, font);
		SizeF sizeF2 = ((unitPart.Length > 0) ? e.Graphics.MeasureString(unitPart, font2) : SizeF.Empty);
		float num = (float)base.Height / 2f;
		float num2 = ((unitPart.Length > 0) ? (sizeF2.Width - 4f) : 0f);
		float num3 = (float)base.Width - num2 - sizeF.Width + 2f;
		e.Graphics.DrawString(numberPart, font, brush, num3, num - sizeF.Height / 2f);
		if (unitPart.Length > 0)
		{
			e.Graphics.DrawString(unitPart, font2, brush, (float)base.Width - sizeF2.Width + 2f, num + sizeF.Height / 2f - sizeF2.Height - 2f);
		}
	}
}
