using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class CardPanel : Panel
{
	private Color? edgeAccent;

	private long[] sparkData;

	private int[] monthCheckStates;

	private int monthCheckStreak;

	private int softwareCheckStreak;

	public bool ShowCheckinIndicators { get; set; }

	public Color? EdgeAccent
	{
		get
		{
			return edgeAccent;
		}
		set
		{
			edgeAccent = value;
			Invalidate();
		}
	}

	public CardPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	public void SetSparkline(long[] values)
	{
		sparkData = values;
		Invalidate();
	}

	public void SetMonthChecks(int[] states, int streak = 0, int softwareStreak = 0)
	{
		monthCheckStreak = Math.Max(0, streak);
		softwareCheckStreak = Math.Max(0, softwareStreak);
		if (states == null || states.Length != 12)
		{
			monthCheckStates = null;
		}
		else
		{
			monthCheckStates = (int[])states.Clone();
		}
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using GraphicsPath graphicsPath = Theme.RoundedRect(new Rectangle(0, 0, base.Width - 1, base.Height - 1), 9);
		using SolidBrush brush = new SolidBrush(Theme.CardBackground);
		e.Graphics.FillPath(brush, graphicsPath);
		e.Graphics.SetClip(graphicsPath);
		DrawMonthChecks(e.Graphics);
		DrawSparkline(e.Graphics);
		if (edgeAccent.HasValue)
		{
			using SolidBrush brush2 = new SolidBrush(edgeAccent.Value);
			e.Graphics.FillRectangle(brush2, 0, 0, 3, base.Height);
		}
		e.Graphics.ResetClip();
		using Pen pen = new Pen(Theme.CardBorder);
		e.Graphics.DrawPath(pen, graphicsPath);
	}

	private void DrawMonthChecks(Graphics graphics)
	{
		if (!ShowCheckinIndicators)
		{
			return;
		}
		int num = 8;
		int num2 = 5;
		int num3 = num * 6 + num2 * 5;
		int num4 = num * 2 + num2;
		int num5 = base.Width - num3 - 12;
		int num6 = base.Height - num4 - 9;
		DrawMonthStreak(graphics, num5, num6 - 21, num3);
		if (monthCheckStates == null || monthCheckStates.Length != 12)
		{
			return;
		}
		for (int i = 0; i < 12; i++)
		{
			int num7 = i % 6;
			int num8 = i / 6;
			Rectangle rect = new Rectangle(num5 + num7 * (num + num2), num6 + num8 * (num + num2), num, num);
			Color color;
			if (monthCheckStates[i] > 0)
			{
				color = Color.FromArgb(150, Theme.Success);
			}
			else if (monthCheckStates[i] == -2)
			{
				color = Color.FromArgb(170, Theme.BenchmarkGold);
			}
			else if (monthCheckStates[i] == -3)
			{
				color = Color.FromArgb(180, 139, 92, 246);
			}
			else if (monthCheckStates[i] < 0)
			{
				color = Color.FromArgb(145, Theme.Error);
			}
			else
			{
				color = Color.FromArgb(85, Theme.TextMuted);
			}
			using GraphicsPath path = Theme.RoundedRect(rect, 2);
			using SolidBrush brush = new SolidBrush(color);
			graphics.FillPath(brush, path);
		}
	}

	private void DrawMonthStreak(Graphics graphics, int x, int y, int width)
	{
		using Font font = new Font("Microsoft YaHei UI", 8.3f, FontStyle.Bold);
		float num = MeasureStreakWidth(graphics, font, monthCheckStreak) + 5f + MeasureStreakWidth(graphics, font, softwareCheckStreak);
		float num2 = x + ((float)width - num) / 2f;
		Color color = Color.FromArgb(145, Theme.TextMuted);
		Color color2 = Color.FromArgb(105, Theme.TrackBackground);
		Color color3 = Color.FromArgb(95, Theme.TextMuted);
		bool flag = monthCheckStreak > 0;
		num2 = DrawStreak(graphics, font, num2, y, monthCheckStreak, flag ? Color.FromArgb(255, 255, 214, 66) : color, flag ? Color.FromArgb(255, 230, 56, 45) : color, flag ? Color.FromArgb(220, 255, 239, 122) : color2);
		num2 += 5f;
		bool flag2 = softwareCheckStreak > 0;
		DrawStreak(graphics, font, num2, y, softwareCheckStreak, flag2 ? Color.FromArgb(255, 96, 165, 250) : color, flag2 ? Color.FromArgb(255, 139, 92, 246) : color, flag2 ? Color.FromArgb(220, 196, 181, 253) : color2, flag2 ? null : color3);
	}

	private static float MeasureStreakWidth(Graphics graphics, Font font, int streak)
	{
		return 17f + graphics.MeasureString(streak.ToString(), font).Width + 4f;
	}

	private static float DrawStreak(Graphics graphics, Font font, float x, int y, int streak, Color topColor, Color bottomColor, Color coreColor, Color? textColor = null)
	{
		string text = streak.ToString();
		SizeF sizeF = graphics.MeasureString(text, font);
		DrawFlameIcon(graphics, x, y + 2, topColor, bottomColor, coreColor);
		RectangleF numberRect = new RectangleF(x + 17f, y, sizeF.Width + 4f, 18f);
		using StringFormat format = new StringFormat();
		format.Alignment = StringAlignment.Near;
		format.LineAlignment = StringAlignment.Center;
		if (textColor.HasValue)
		{
			using SolidBrush brush = new SolidBrush(textColor.Value);
			graphics.DrawString(text, font, brush, numberRect, format);
		}
		else
		{
			using LinearGradientBrush brush = new LinearGradientBrush(numberRect, topColor, bottomColor, LinearGradientMode.Vertical);
			graphics.DrawString(text, font, brush, numberRect, format);
		}
		return x + 17f + sizeF.Width + 4f;
	}

	private static void DrawFlameIcon(Graphics graphics, float x, float y, Color topColor, Color bottomColor, Color coreColor)
	{
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddBezier(x + 7f, y + 15f, x - 2f, y + 8f, x + 5f, y + 5f, x + 6f, y);
		graphicsPath.AddBezier(x + 7f, y + 4f, x + 13f, y + 5f, x + 16f, y + 10f, x + 9f, y + 15f);
		graphicsPath.CloseFigure();
		RectangleF rect = new RectangleF(x, y, 16f, 16f);
		using LinearGradientBrush brush = new LinearGradientBrush(rect, topColor, bottomColor, LinearGradientMode.Vertical);
		graphics.FillPath(brush, graphicsPath);
		using GraphicsPath graphicsPath2 = new GraphicsPath();
		graphicsPath2.AddBezier(x + 8f, y + 13f, x + 4f, y + 9f, x + 8f, y + 7f, x + 8f, y + 4f);
		graphicsPath2.AddBezier(x + 9f, y + 7f, x + 12f, y + 9f, x + 12f, y + 12f, x + 8f, y + 13f);
		graphicsPath2.CloseFigure();
		using SolidBrush brush2 = new SolidBrush(coreColor);
		graphics.FillPath(brush2, graphicsPath2);
	}

	private void DrawSparkline(Graphics graphics)
	{
		if (sparkData == null || sparkData.Length < 3)
		{
			return;
		}
		long num = long.MaxValue;
		long num2 = long.MinValue;
		long[] array = sparkData;
		foreach (long num3 in array)
		{
			if (num3 < num)
			{
				num = num3;
			}
			if (num3 > num2)
			{
				num2 = num3;
			}
		}
		if (num2 <= num)
		{
			num2 = num + 1;
		}
		Color baseColor = ((sparkData[sparkData.Length - 1] >= sparkData[0]) ? Theme.Success : Theme.Error);
		float num4 = (float)base.Width * 0.42f;
		float num5 = (float)base.Width - 10f;
		float num6 = (float)base.Height * 0.42f;
		float num7 = (float)base.Height - 8f;
		PointF[] array2 = new PointF[sparkData.Length];
		for (int j = 0; j < sparkData.Length; j++)
		{
			float num8 = num4 + (num5 - num4) * ((sparkData.Length == 1) ? 0f : ((float)j / (float)(sparkData.Length - 1)));
			float num9 = num7 - (num7 - num6) * ((float)(sparkData[j] - num) / (float)(num2 - num));
			array2[j] = new PointF(num8, num9);
		}
		PointF[] array3 = new PointF[array2.Length + 2];
		array2.CopyTo(array3, 0);
		array3[array2.Length] = new PointF(num5, num7);
		array3[array2.Length + 1] = new PointF(num4, num7);
		using (SolidBrush brush = new SolidBrush(Color.FromArgb(16, baseColor)))
		{
			graphics.FillPolygon(brush, array3);
		}
		using Pen pen = new Pen(Color.FromArgb(58, baseColor), 1.4f);
		graphics.DrawLines(pen, array2);
	}
}
