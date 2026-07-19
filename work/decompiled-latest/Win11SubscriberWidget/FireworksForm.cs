using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class FireworksForm : Form
{
	private sealed class Particle
	{
		public float X;

		public float Y;

		public float PreviousX;

		public float PreviousY;

		public float OlderX;

		public float OlderY;

		public float VelocityX;

		public float VelocityY;

		public float Gravity;

		public float Drag;

		public int Delay;

		public int Life;

		public int MaxLife;

		public float Size;

		public bool Twinkle;

		public int Shape;

		public Color Color;
	}

	private static readonly Color[] Palette = new Color[12]
	{
		Theme.BiliAccent,
		Theme.YouTubeAccent,
		Theme.BenchmarkGold,
		Theme.ProgressAhead,
		Color.FromArgb(167, 139, 250),
		Color.FromArgb(244, 114, 182),
		Color.FromArgb(251, 146, 60),
		Color.FromArgb(34, 211, 238),
		Color.FromArgb(163, 230, 53),
		Color.FromArgb(96, 165, 250),
		Color.FromArgb(248, 113, 113),
		Color.White
	};

	private static readonly Random CelebrationRandom = new Random();

	private readonly List<Particle> particles = new List<Particle>();

	private readonly Random random = new Random();

	private readonly Timer animationTimer;

	private readonly string celebrationText;

	private readonly DateTime startedAt;

	private int frame;

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams obj = base.CreateParams;
			obj.ExStyle |= 32;
			obj.ExStyle |= 128;
			obj.ExStyle |= 134217728;
			return obj;
		}
	}

	private FireworksForm(Rectangle bounds, int burstCount, string text)
	{
		Text = AppInfo.DisplayName + " · 成就烟花";
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		base.StartPosition = FormStartPosition.Manual;
		base.AutoScaleMode = AutoScaleMode.Dpi;
		base.Bounds = bounds;
		base.TopMost = true;
		BackColor = Color.Black;
		base.TransparencyKey = Color.Black;
		DoubleBuffered = true;
		celebrationText = text;
		startedAt = DateTime.UtcNow;
		SpawnBursts(Math.Max(1, Math.Min(7, burstCount)));
		animationTimer = new Timer();
		animationTimer.Interval = 20;
		animationTimer.Tick += AnimationTick;
	}

	public static void ShowCelebration(Form owner, int achievementCount)
	{
		string text = ((achievementCount > 1) ? ("成就达成 ×" + achievementCount) : "成就达成！");
		ShowFireworks(owner, Math.Max(1, Math.Min(4, achievementCount)), text);
	}

	public static void ShowRandomCelebration(Form owner)
	{
		int burstCount;
		lock (CelebrationRandom)
		{
			burstCount = CelebrationRandom.Next(4, 8);
		}
		ShowFireworks(owner, burstCount, "");
	}

	private static void ShowFireworks(Form owner, int burstCount, string text)
	{
		if (owner != null && !owner.IsDisposed && burstCount > 0)
		{
			Rectangle bounds = Screen.FromControl(owner).Bounds;
			FireworksForm fireworks = new FireworksForm(bounds, burstCount, text);
			fireworks.FormClosed += delegate
			{
				fireworks.Dispose();
			};
			fireworks.Show();
			fireworks.animationTimer.Start();
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
		e.Graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
		foreach (Particle particle in particles)
		{
			if (particle.Delay > 0)
			{
				continue;
			}
			float num = Math.Max(0f, (float)particle.Life / (float)particle.MaxLife);
			float num2 = (particle.Twinkle ? (0.72f + 0.38f * (float)Math.Abs(Math.Sin((double)(frame + particle.Life) * 0.34))) : 1f);
			float num3 = particle.Size * num2;
			using (Pen pen = new Pen(FadeColor(particle.Color, num * 0.34f), Math.Max(1f, num3 * 0.3f)))
			{
				e.Graphics.DrawLine(pen, particle.OlderX, particle.OlderY, particle.PreviousX, particle.PreviousY);
			}
			using (Pen pen2 = new Pen(FadeColor(particle.Color, num * 0.78f), Math.Max(1f, num3 * 0.52f)))
			{
				e.Graphics.DrawLine(pen2, particle.PreviousX, particle.PreviousY, particle.X, particle.Y);
			}
			using SolidBrush brush = new SolidBrush(FadeColor(particle.Color, num));
			if (particle.Shape == 1)
			{
				using Pen pen3 = new Pen(FadeColor(particle.Color, num), Math.Max(1f, num3 * 0.42f));
				e.Graphics.DrawLine(pen3, particle.X - num3, particle.Y, particle.X + num3, particle.Y);
				e.Graphics.DrawLine(pen3, particle.X, particle.Y - num3, particle.X, particle.Y + num3);
			}
			else if (particle.Shape == 2)
			{
				PointF[] points = new PointF[4]
				{
					new PointF(particle.X, particle.Y - num3),
					new PointF(particle.X + num3 * 0.7f, particle.Y),
					new PointF(particle.X, particle.Y + num3),
					new PointF(particle.X - num3 * 0.7f, particle.Y)
				};
				e.Graphics.FillPolygon(brush, points);
			}
			else
			{
				e.Graphics.FillEllipse(brush, particle.X - num3 / 2f, particle.Y - num3 / 2f, num3, num3);
			}
			using SolidBrush brush2 = new SolidBrush(FadeColor(Color.White, num * 0.9f));
			e.Graphics.FillEllipse(brush2, particle.X - num3 * 0.16f, particle.Y - num3 * 0.16f, num3 * 0.32f, num3 * 0.32f);
		}
		float num4 = ((frame < 135) ? Math.Min(1f, (float)frame / 14f) : Math.Max(0f, (205f - (float)frame) / 70f));
		if (string.IsNullOrEmpty(celebrationText) || !(num4 > 0f))
		{
			return;
		}
		using Font font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold);
		using StringFormat format = new StringFormat
		{
			Alignment = StringAlignment.Center,
			LineAlignment = StringAlignment.Center
		};
		RectangleF layoutRectangle = new RectangleF(0f, (float)base.Height - 54f, base.Width, 42f);
		using SolidBrush brush3 = new SolidBrush(Color.FromArgb((int)(150f * num4), Color.Black));
		e.Graphics.DrawString(celebrationText, font, brush3, new RectangleF(layoutRectangle.X + 2f, layoutRectangle.Y + 2f, layoutRectangle.Width, layoutRectangle.Height), format);
		Color baseColor = Palette[frame / 10 % Palette.Length];
		using SolidBrush brush4 = new SolidBrush(Color.FromArgb((int)(255f * num4), baseColor));
		e.Graphics.DrawString(celebrationText, font, brush4, layoutRectangle, format);
	}

	private static Color FadeColor(Color color, float brightness)
	{
		brightness = Math.Max(0.015f, Math.Min(1f, brightness));
		return Color.FromArgb(255, Math.Max(1, (int)((float)(int)color.R * brightness)), Math.Max(1, (int)((float)(int)color.G * brightness)), Math.Max(1, (int)((float)(int)color.B * brightness)));
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		animationTimer.Stop();
		animationTimer.Dispose();
		base.OnFormClosed(e);
	}

	private void AnimationTick(object sender, EventArgs e)
	{
		frame++;
		for (int num = particles.Count - 1; num >= 0; num--)
		{
			Particle particle = particles[num];
			if (particle.Delay > 0)
			{
				particle.Delay--;
			}
			else
			{
				particle.OlderX = particle.PreviousX;
				particle.OlderY = particle.PreviousY;
				particle.PreviousX = particle.X;
				particle.PreviousY = particle.Y;
				particle.X += particle.VelocityX;
				particle.Y += particle.VelocityY;
				particle.VelocityX *= particle.Drag;
				particle.VelocityY = particle.VelocityY * particle.Drag + particle.Gravity;
				particle.Life--;
				if (particle.Life <= 0)
				{
					particles.RemoveAt(num);
				}
			}
		}
		Invalidate();
		if ((DateTime.UtcNow - startedAt).TotalSeconds >= 4.5 || particles.Count == 0)
		{
			Close();
		}
	}

	private void SpawnBursts(int burstCount)
	{
		for (int i = 0; i < burstCount; i++)
		{
			int num = random.Next(5);
			float num2 = (float)base.Width * (float)(i + 1) / (float)(burstCount + 1) + (float)random.Next(-60, 61);
			float num3 = ((num == 4) ? ((float)base.Height - 42f) : ((float)base.Height * (0.18f + (float)random.NextDouble() * 0.46f)));
			Color color = Palette[random.Next(Palette.Length)];
			int num4 = ((num == 4) ? (88 + random.Next(18)) : (68 + random.Next(20)));
			for (int j = 0; j < num4; j++)
			{
				double num5 = Math.PI * 2.0 * (double)j / (double)num4;
				float gravity = 0.045f;
				float velocityX;
				float velocityY;
				switch (num)
				{
				case 1:
				{
					float num11 = 4.4f + (float)random.NextDouble() * 0.8f;
					velocityX = (float)Math.Cos(num5) * num11;
					velocityY = (float)Math.Sin(num5) * num11;
					break;
				}
				case 2:
				{
					float num9 = 16f * (float)Math.Pow(Math.Sin(num5), 3.0);
					float num10 = 13f * (float)Math.Cos(num5) - 5f * (float)Math.Cos(2.0 * num5) - 2f * (float)Math.Cos(3.0 * num5) - (float)Math.Cos(4.0 * num5);
					velocityX = num9 * 0.34f;
					velocityY = (0f - num10) * 0.3f;
					gravity = 0.028f;
					break;
				}
				case 3:
				{
					float num7 = 0.42f + 0.58f * (float)Math.Abs(Math.Cos(num5 * 5.0));
					float num8 = 2.2f + 4.6f * num7;
					velocityX = (float)Math.Cos(num5 - Math.PI / 2.0) * num8;
					velocityY = (float)Math.Sin(num5 - Math.PI / 2.0) * num8;
					gravity = 0.03f;
					break;
				}
				case 4:
					velocityX = -3.1f + (float)random.NextDouble() * 6.2f;
					velocityY = 0f - (4.6f + (float)random.NextDouble() * 6.2f);
					gravity = 0.095f;
					break;
				default:
				{
					num5 += random.NextDouble() * 0.11;
					float num6 = 2.2f + (float)random.NextDouble() * 5.3f;
					velocityX = (float)Math.Cos(num5) * num6;
					velocityY = (float)Math.Sin(num5) * num6;
					break;
				}
				}
				Color color2 = ((j % 3 == 0) ? Palette[random.Next(Palette.Length)] : color);
				int num12 = ((num == 4) ? (90 + random.Next(38)) : (72 + random.Next(42)));
				particles.Add(new Particle
				{
					X = num2,
					Y = num3,
					PreviousX = num2,
					PreviousY = num3,
					OlderX = num2,
					OlderY = num3,
					VelocityX = velocityX,
					VelocityY = velocityY,
					Gravity = gravity,
					Drag = 0.987f,
					Delay = i * 11 + random.Next(0, (num == 4) ? 28 : 6),
					Life = num12,
					MaxLife = num12,
					Size = 2.3f + (float)random.NextDouble() * 3.4f,
					Twinkle = (j % 4 == 0),
					Shape = random.Next(3),
					Color = color2
				});
			}
		}
	}
}
