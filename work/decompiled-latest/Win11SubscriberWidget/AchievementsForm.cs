using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal class AchievementsForm : Form
{
	private class EventEntry
	{
		public string Time;

		public string Tag;

		public string Content;
	}

	private const int MaxEntries = 200;

	private readonly ToolTip contentToolTip = new ToolTip
	{
		AutoPopDelay = 12000,
		InitialDelay = 300
	};

	public AchievementsForm()
	{
		Text = AppInfo.DisplayName + " · 成就记录";
		base.StartPosition = FormStartPosition.CenterParent;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MinimizeBox = false;
		base.MaximizeBox = false;
		base.ShowInTaskbar = false;
		base.AutoScaleMode = AutoScaleMode.Dpi;
		base.ClientSize = new Size(478, 560);
		BackColor = Theme.PanelBackground;
		ForeColor = Theme.TextPrimary;
		Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
		BuildUi();
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		NativeMethods.ApplyDarkTitleBar(base.Handle);
		NativeMethods.ApplyRoundedCorners(base.Handle);
	}

	private void BuildUi()
	{
		List<EventEntry> list = LoadEntries();
		Panel panel = new Panel();
		panel.Dock = DockStyle.Top;
		panel.Height = 40;
		panel.BackColor = Theme.PanelBackground;
		panel.Padding = new Padding(18, 10, 18, 4);
		base.Controls.Add(panel);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		using (List<EventEntry>.Enumerator enumerator = list.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current.Tag)
				{
				case "里程碑":
					num++;
					break;
				case "异动":
					num2++;
					break;
				case "预警":
					num3++;
					break;
				case "周报":
					num4++;
					break;
				}
			}
		}
		int num5 = 18;
		num5 = AddStat(panel, num5, "★ 里程碑 " + num, Theme.BenchmarkGold);
		num5 = AddStat(panel, num5, "△ 异动 " + num2, Theme.BiliAccent);
		num5 = AddStat(panel, num5, "! 预警 " + num3, Theme.Error);
		AddStat(panel, num5, "周报 " + num4, Theme.Success);
		Panel panel2 = new Panel();
		panel2.Dock = DockStyle.Bottom;
		panel2.Height = 44;
		panel2.BackColor = Theme.PanelBackground;
		base.Controls.Add(panel2);
		Label openLogLink = new Label();
		openLogLink.Text = "打开原始日志";
		openLogLink.ForeColor = Theme.TextMuted;
		openLogLink.Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Underline);
		openLogLink.AutoSize = true;
		openLogLink.Cursor = Cursors.Hand;
		openLogLink.Location = new Point(18, 14);
		openLogLink.MouseEnter += delegate
		{
			openLogLink.ForeColor = Theme.TextPrimary;
		};
		openLogLink.MouseLeave += delegate
		{
			openLogLink.ForeColor = Theme.TextMuted;
		};
		openLogLink.Click += delegate
		{
			try
			{
				Process.Start("notepad.exe", "\"" + MilestoneTracker.EventLogPath + "\"");
			}
			catch (Exception ex)
			{
				AppLogger.Error("achievement-log-open", ex);
			}
		};
		panel2.Controls.Add(openLogLink);
		Button button = new Button();
		button.Text = "关闭";
		button.Width = 88;
		button.Height = 30;
		button.FlatStyle = FlatStyle.Flat;
		button.BackColor = Theme.InputBackground;
		button.ForeColor = Theme.TextSecondary;
		button.FlatAppearance.BorderColor = Theme.CardBorder;
		button.FlatAppearance.MouseOverBackColor = Theme.TrackBackground;
		button.Cursor = Cursors.Hand;
		button.Location = new Point(base.ClientSize.Width - 106, 8);
		button.Click += delegate
		{
			Close();
		};
		panel2.Controls.Add(button);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.WrapContents = false;
		flowLayoutPanel.AutoScroll = true;
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.BackColor = Theme.PanelBackground;
		flowLayoutPanel.Padding = new Padding(14, 4, 4, 4);
		base.Controls.Add(flowLayoutPanel);
		flowLayoutPanel.BringToFront();
		if (list.Count == 0)
		{
			Label label = new Label();
			label.Text = "还没有成就记录。\n达成里程碑、粉丝异动、周报都会记在这里——继续加油！";
			label.ForeColor = Theme.TextMuted;
			label.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
			label.AutoSize = true;
			label.Margin = new Padding(6, 24, 0, 0);
			flowLayoutPanel.Controls.Add(label);
			return;
		}
		foreach (EventEntry item in list)
		{
			flowLayoutPanel.Controls.Add(CreateEntryCard(item, flowLayoutPanel));
		}
	}

	private int AddStat(Panel host, int x, string text, Color color)
	{
		Label label = new Label();
		label.Text = text;
		label.ForeColor = color;
		label.Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Bold);
		label.AutoSize = true;
		label.Location = new Point(x, 12);
		host.Controls.Add(label);
		return x + label.PreferredWidth + 16;
	}

	private Control CreateEntryCard(EventEntry entry, FlowLayoutPanel host)
	{
		Color badgeColor;
		string text;
		switch (entry.Tag)
		{
		case "里程碑":
			badgeColor = Theme.BenchmarkGold;
			text = "★";
			break;
		case "异动":
			badgeColor = Theme.BiliAccent;
			text = "△";
			break;
		case "预警":
			badgeColor = Theme.Error;
			text = "!";
			break;
		case "周报":
			badgeColor = Theme.Success;
			text = "周";
			break;
		default:
			badgeColor = Theme.TextMuted;
			text = "·";
			break;
		}
		CardPanel cardPanel = new CardPanel();
		cardPanel.BackColor = Theme.PanelBackground;
		cardPanel.Size = new Size(438, 46);
		cardPanel.Margin = new Padding(0, 4, 0, 4);
		BadgeLabel badgeLabel = new BadgeLabel();
		badgeLabel.Text = text;
		badgeLabel.BadgeColor = badgeColor;
		badgeLabel.ForeColor = ((entry.Tag == "里程碑") ? Color.FromArgb(60, 45, 0) : Color.White);
		badgeLabel.BackColor = Theme.CardBackground;
		badgeLabel.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
		badgeLabel.Size = new Size(24, 24);
		badgeLabel.Location = new Point(11, 11);
		cardPanel.Controls.Add(badgeLabel);
		Label label = new Label();
		label.Text = entry.Content;
		label.ForeColor = Theme.TextSecondary;
		label.BackColor = Theme.CardBackground;
		label.Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular);
		label.AutoEllipsis = true;
		label.Size = new Size(cardPanel.Width - 48 - 78, 32);
		label.Location = new Point(44, 7);
		label.TextAlign = ContentAlignment.MiddleLeft;
		cardPanel.Controls.Add(label);
		contentToolTip.SetToolTip(label, entry.Content);
		Label label2 = new Label();
		label2.Text = entry.Time;
		label2.ForeColor = Theme.TextMuted;
		label2.BackColor = Theme.CardBackground;
		label2.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
		label2.Size = new Size(72, 30);
		label2.TextAlign = ContentAlignment.MiddleRight;
		label2.Location = new Point(cardPanel.Width - 82, 8);
		cardPanel.Controls.Add(label2);
		if (entry.Tag == "里程碑")
		{
			cardPanel.Cursor = Cursors.Hand;
			badgeLabel.Cursor = Cursors.Hand;
			label.Cursor = Cursors.Hand;
			label2.Cursor = Cursors.Hand;
			EventHandler value = delegate
			{
				FireworksForm.ShowRandomCelebration(this);
			};
			cardPanel.Click += value;
			badgeLabel.Click += value;
			label.Click += value;
			label2.Click += value;
			contentToolTip.SetToolTip(cardPanel, "点击重放随机烟花");
			contentToolTip.SetToolTip(badgeLabel, "点击重放随机烟花");
			contentToolTip.SetToolTip(label, entry.Content + "\n点击重放随机烟花");
			contentToolTip.SetToolTip(label2, "点击重放随机烟花");
		}
		return cardPanel;
	}

	private static List<EventEntry> LoadEntries()
	{
		List<EventEntry> list = new List<EventEntry>();
		try
		{
			if (!File.Exists(MilestoneTracker.EventLogPath))
			{
				return list;
			}
			string[] array = File.ReadAllLines(MilestoneTracker.EventLogPath);
			int num = array.Length - 1;
			while (num >= 0 && list.Count < 200)
			{
				EventEntry eventEntry = ParseLine(array[num]);
				if (eventEntry != null)
				{
					list.Add(eventEntry);
				}
				num--;
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("achievement-log-read", ex);
		}
		return list;
	}

	private static EventEntry ParseLine(string line)
	{
		if (string.IsNullOrEmpty(line) || line.Length < 21)
		{
			return null;
		}
		int num = line.IndexOf('[');
		int num2 = line.IndexOf(']');
		if (num < 0 || num2 <= num)
		{
			return null;
		}
		if (!DateTime.TryParse(line.Substring(0, num).Trim(), out var result))
		{
			return null;
		}
		return new EventEntry
		{
			Time = result.ToString("MM-dd HH:mm"),
			Tag = line.Substring(num + 1, num2 - num - 1),
			Content = line.Substring(num2 + 1).Trim()
		};
	}
}
