using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class SegmentedTextLabel : Label
{
	private struct Segment
	{
		public string Text;

		public Color Color;
	}

	private readonly List<Segment> segments = new List<Segment>();

	public SegmentedTextLabel()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, value: true);
	}

	public void SetSegments(params Tuple<string, Color>[] values)
	{
		segments.Clear();
		if (values != null)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] != null && !string.IsNullOrEmpty(values[i].Item1))
				{
					segments.Add(new Segment
					{
						Text = values[i].Item1,
						Color = values[i].Item2
					});
				}
			}
		}
		Invalidate();
	}

	protected override void OnTextChanged(EventArgs e)
	{
		segments.Clear();
		base.OnTextChanged(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (segments.Count == 0)
		{
			base.OnPaint(e);
			return;
		}
		e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		using StringFormat stringFormat = new StringFormat(StringFormatFlags.NoWrap);
		stringFormat.Trimming = StringTrimming.EllipsisCharacter;
		float num = 0f;
		float y = Math.Max(0f, ((float)Height - Font.GetHeight(e.Graphics)) / 2f - 1f);
		for (int i = 0; i < segments.Count; i++)
		{
			Segment segment = segments[i];
			float num2 = Math.Max(0f, (float)Width - num);
			if (num2 <= 1f)
			{
				break;
			}
			using SolidBrush brush = new SolidBrush(segment.Color);
			e.Graphics.DrawString(segment.Text, Font, brush, new RectangleF(num, y, num2, Height - y), stringFormat);
			SizeF sizeF = e.Graphics.MeasureString(segment.Text, Font, int.MaxValue, StringFormat.GenericTypographic);
			num += Math.Max(0f, sizeF.Width - 1f);
		}
	}
}
