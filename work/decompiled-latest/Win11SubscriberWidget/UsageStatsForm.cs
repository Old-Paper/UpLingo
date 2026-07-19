using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class UsageStatsForm : Form
{
	private readonly WidgetConfig config;

	public UsageStatsForm(WidgetConfig source)
	{
		config = source ?? new WidgetConfig();
		config.ApplyDefaults();
		Text = AppInfo.DisplayName + " · 使用与打卡统计";
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MinimizeBox = false;
		MaximizeBox = false;
		ShowInTaskbar = false;
		AutoScaleMode = AutoScaleMode.Dpi;
		ClientSize = new Size(548, 640);
		BackColor = Theme.PanelBackground;
		ForeColor = Theme.TextPrimary;
		Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
		BuildUi();
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		NativeMethods.ApplyDarkTitleBar(Handle);
		NativeMethods.ApplyRoundedCorners(Handle);
	}

	private void BuildUi()
	{
		List<UsageStatConfig> usedRecords = UsageStatsService.GetUsedRecords(config);
		Panel header = new Panel
		{
			Dock = DockStyle.Top,
			Height = 126,
			BackColor = Theme.PanelBackground,
			Padding = new Padding(18, 0, 18, 0)
		};
		Controls.Add(header);
		Label title = new Label
		{
			Text = "使用与打卡统计",
			ForeColor = Theme.TextPrimary,
			BackColor = header.BackColor,
			Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(18, 15)
		};
		header.Controls.Add(title);
		Label subtitle = new Label
		{
			Text = "本地汇总前台活跃时长、每日打卡与补签卡",
			ForeColor = Theme.TextMuted,
			BackColor = header.BackColor,
			Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Regular),
			AutoSize = true,
			Location = new Point(19, 43)
		};
		header.Controls.Add(subtitle);
		header.Controls.Add(CreateSummaryCard("软件累计", UsageStatsService.FormatDuration(UsageStatsService.TotalSeconds(config)), Theme.BenchmarkGold, new Point(18, 72)));
		header.Controls.Add(CreateSummaryCard("今天", UsageStatsService.FormatDuration(UsageStatsService.TodaySeconds(config, DateTime.Now)), Theme.Success, new Point(190, 72)));
		header.Controls.Add(CreateSummaryCard("已记录", usedRecords.Count + " 款软件", Theme.BiliAccent, new Point(362, 72)));

		Panel footer = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 48,
			BackColor = Theme.PanelBackground
		};
		Controls.Add(footer);
		Label note = new Label
		{
			Text = "数据仅保存在本机 · 未使用的软件不会显示",
			ForeColor = Theme.TextMuted,
			BackColor = footer.BackColor,
			Font = new Font("Microsoft YaHei UI", 7.5f, FontStyle.Regular),
			AutoSize = true,
			Location = new Point(18, 17)
		};
		footer.Controls.Add(note);
		Button closeButton = new Button
		{
			Text = "关闭",
			Width = 88,
			Height = 30,
			FlatStyle = FlatStyle.Flat,
			BackColor = Theme.InputBackground,
			ForeColor = Theme.TextSecondary,
			Cursor = Cursors.Hand,
			Location = new Point(ClientSize.Width - 106, 9)
		};
		closeButton.FlatAppearance.BorderColor = Theme.CardBorder;
		closeButton.FlatAppearance.MouseOverBackColor = Theme.TrackBackground;
		closeButton.Click += delegate { Close(); };
		footer.Controls.Add(closeButton);

		FlowLayoutPanel list = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = true,
			AutoScroll = true,
			BackColor = Theme.PanelBackground,
			Padding = new Padding(16, 8, 8, 8)
		};
		Controls.Add(list);
		list.BringToFront();
		if (usedRecords.Count == 0)
		{
			list.Controls.Add(CreateProfessionalCheckinCard());
			list.Controls.Add(CreateCreatorCheckinCard());
			list.Controls.Add(CreateEmptyState());
			return;
		}
		list.Controls.Add(CreateProfessionalCheckinCard());
		list.Controls.Add(CreateCreatorCheckinCard());
		foreach (UsageStatConfig record in usedRecords)
		{
			list.Controls.Add(CreateUsageCard(record));
		}
	}

	private static Control CreateSummaryCard(string title, string value, Color accent, Point location)
	{
		CardPanel card = new CardPanel
		{
			Size = new Size(160, 42),
			Location = location,
			BackColor = Theme.PanelBackground,
			EdgeAccent = accent
		};
		Label label = new Label
		{
			Text = title,
			ForeColor = Theme.TextMuted,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 7f, FontStyle.Regular),
			AutoSize = true,
			Location = new Point(11, 6)
		};
		card.Controls.Add(label);
		Label label2 = new Label
		{
			Text = value,
			ForeColor = Theme.TextPrimary,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Bold),
			AutoEllipsis = true,
			Size = new Size(140, 18),
			Location = new Point(11, 19)
		};
		card.Controls.Add(label2);
		return card;
	}

	private static Control CreateUsageCard(UsageStatConfig record)
	{
		ProfessionalAppDefinition app = ProfessionalAppCatalog.Find(record.app_id);
		Color color = app?.Accent ?? Theme.TextMuted;
		string text = string.IsNullOrEmpty(record.label) ? (app?.Label ?? "专业软件") : record.label;
		CardPanel card = new CardPanel
		{
			Size = new Size(248, 86),
			Margin = new Padding(0, 0, 8, 8),
			BackColor = Theme.PanelBackground,
			EdgeAccent = color
		};
		Label label = new Label
		{
			Text = "●",
			ForeColor = color,
			BackColor = Color.Transparent,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			Size = new Size(20, 20),
			Location = new Point(12, 11),
			TextAlign = ContentAlignment.MiddleCenter
		};
		card.Controls.Add(label);
		Label label2 = new Label
		{
			Text = text,
			ForeColor = Theme.TextSecondary,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Bold),
			AutoEllipsis = true,
			Size = new Size(130, 20),
			Location = new Point(37, 10)
		};
		card.Controls.Add(label2);
		Label label3 = new Label
		{
			Text = UsageStatsService.FormatDuration(record.total_seconds),
			ForeColor = Theme.TextPrimary,
			BackColor = Color.Transparent,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			AutoEllipsis = true,
			Size = new Size(102, 24),
			Location = new Point(136, 8),
			TextAlign = ContentAlignment.MiddleRight
		};
		card.Controls.Add(label3);
		Label label4 = new Label
		{
			Text = "今天 " + UsageStatsService.FormatDuration(IsToday(record) ? record.today_seconds : 0L),
			ForeColor = color,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 7.5f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(37, 35)
		};
		card.Controls.Add(label4);
		Label label5 = new Label
		{
			Text = FormatLastUsed(record.last_used_at),
			ForeColor = Theme.TextMuted,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 7f, FontStyle.Regular),
			AutoEllipsis = true,
			Size = new Size(198, 18),
			Location = new Point(37, 57)
		};
		card.Controls.Add(label5);
		return card;
	}

	private Control CreateProfessionalCheckinCard()
	{
		DateTime now = DateTime.Now;
		int streak = ProfessionalCheckinService.GetStreak(config, now);
		bool checkedIn = ProfessionalCheckinService.IsCheckedIn(config, now);
		CardPanel card = new CardPanel
		{
			Size = new Size(500, 138),
			Margin = new Padding(0, 0, 0, 8),
			BackColor = Theme.PanelBackground,
			EdgeAccent = Color.FromArgb(139, 92, 246)
		};
		AddCheckinTitle(card, "专业软件打卡", "蓝紫连胜 " + streak + " 天" + (checkedIn ? " · 今日已打卡" : " · 今日待续"), Color.FromArgb(139, 92, 246));
		Label label = new Label
		{
			Text = "专业补签卡 " + config.professional_makeup_cards,
			ForeColor = Color.FromArgb(147, 197, 253),
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(333, 11)
		};
		card.Controls.Add(label);
		Button button = CreateCheckinButton("补签漏签日", new Point(356, 35));
		button.Enabled = config.professional_makeup_cards > 0;
		button.Click += delegate
		{
			UseProfessionalMakeupCard();
		};
		card.Controls.Add(button);
		DateTime dateTime = now.Date.AddDays(-13.0);
		for (int i = 0; i < 14; i++)
		{
			DateTime dateTime2 = dateTime.AddDays(i);
			ProfessionalCheckinConfig professionalCheckinConfig = ProfessionalCheckinService.Find(config, dateTime2);
			int state = GetProfessionalState(professionalCheckinConfig, dateTime2, now);
			BadgeLabel badgeLabel = CreateCheckCell(dateTime2.Day.ToString(), state, new Point(16 + i * 33, 72));
			card.Controls.Add(badgeLabel);
		}
		Label label2 = new Label
		{
			Text = "绿=真实打开  紫=补签  红=漏签  黄=今天待打卡",
			ForeColor = Theme.TextMuted,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 7f, FontStyle.Regular),
			AutoSize = true,
			Location = new Point(16, 108)
		};
		card.Controls.Add(label2);
		return card;
	}

	private Control CreateCreatorCheckinCard()
	{
		DateTime now = DateTime.Now;
		string text = config.creator_state?.channel_key ?? "";
		bool flag = !string.IsNullOrEmpty(text);
		int num = flag ? WidgetRules.CalculateMonthlyStreak(config.monthly_updates, text, now) : 0;
		CardPanel card = new CardPanel
		{
			Size = new Size(500, 132),
			Margin = new Padding(0, 0, 0, 8),
			BackColor = Theme.PanelBackground,
			EdgeAccent = Color.FromArgb(230, 56, 45)
		};
		AddCheckinTitle(card, "投稿打卡", "红黄连胜 " + num + " 个月", Color.FromArgb(245, 158, 11));
		Label label = new Label
		{
			Text = "投稿补签卡 " + config.creator_makeup_cards,
			ForeColor = Theme.Warning,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(333, 11)
		};
		card.Controls.Add(label);
		Button button = CreateCheckinButton("补签漏签月", new Point(356, 35));
		button.Enabled = flag && config.creator_makeup_cards > 0 && HasCreatorMakeupCandidate(text, now);
		button.Click += delegate
		{
			UseCreatorMakeupCard();
		};
		card.Controls.Add(button);
		bool flag2 = flag && IsCreatorHistoryComplete(text, now.Year);
		for (int i = 1; i <= 12; i++)
		{
			int state;
			if (i > now.Month)
			{
				state = 0;
			}
			else
			{
				state = GetCreatorState(text, now.Year, i, now.Month, flag2);
			}
			BadgeLabel badgeLabel = CreateCheckCell(i.ToString(), state, new Point(16 + (i - 1) * 28, 72));
			badgeLabel.Size = new Size(22, 22);
			card.Controls.Add(badgeLabel);
		}
		Label label2 = new Label
		{
			Text = flag ? "绿=真实投稿  紫=补签  红=漏投  黄=本月待投稿" : "完成一次 YouTube 刷新后，这里会显示投稿打卡。",
			ForeColor = Theme.TextMuted,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 7f, FontStyle.Regular),
			AutoSize = true,
			Location = new Point(16, 106)
		};
		card.Controls.Add(label2);
		return card;
	}

	private static void AddCheckinTitle(Control card, string title, string detail, Color detailColor)
	{
		Label label = new Label
		{
			Text = title,
			ForeColor = Theme.TextPrimary,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(16, 10)
		};
		card.Controls.Add(label);
		Label label2 = new Label
		{
			Text = detail,
			ForeColor = detailColor,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 7.5f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(16, 34)
		};
		card.Controls.Add(label2);
	}

	private static Button CreateCheckinButton(string text, Point location)
	{
		Button button = new Button
		{
			Text = text,
			Width = 126,
			Height = 27,
			Location = location,
			FlatStyle = FlatStyle.Flat,
			BackColor = Theme.InputBackground,
			ForeColor = Theme.TextSecondary,
			Font = new Font("Microsoft YaHei UI", 7.5f, FontStyle.Bold),
			Cursor = Cursors.Hand
		};
		button.FlatAppearance.BorderColor = Theme.CardBorder;
		button.FlatAppearance.MouseOverBackColor = Theme.TrackBackground;
		return button;
	}

	private static BadgeLabel CreateCheckCell(string text, int state, Point location)
	{
		Color color = (state > 0) ? Color.FromArgb(120, Theme.Success) : ((state == -3) ? Color.FromArgb(190, 139, 92, 246) : ((state == -2) ? Color.FromArgb(190, Theme.BenchmarkGold) : ((state < 0) ? Color.FromArgb(120, Theme.Error) : Color.FromArgb(80, Theme.TextMuted))));
		return new BadgeLabel
		{
			Text = text,
			BadgeColor = color,
			ForeColor = ((state == -2) ? Color.FromArgb(64, 45, 0) : Color.White),
			BackColor = Color.Transparent,
			Font = new Font("Segoe UI", 7f, FontStyle.Bold),
			Size = new Size(24, 24),
			Location = location
		};
	}

	private static int GetProfessionalState(ProfessionalCheckinConfig record, DateTime date, DateTime now)
	{
		if (record != null && record.opened)
		{
			return 1;
		}
		if (record != null && record.is_makeup)
		{
			return -3;
		}
		return (date.Date == now.Date) ? (-2) : (-1);
	}

	private bool IsCreatorHistoryComplete(string channelKey, int year)
	{
		return config.creator_state != null && config.creator_state.monthly_history_complete && config.creator_state.monthly_history_year == year && string.Equals(config.creator_state.channel_key, channelKey, StringComparison.OrdinalIgnoreCase);
	}

	private int GetCreatorState(string channelKey, int year, int month, int currentMonth, bool historyComplete)
	{
		if (WidgetRules.HasMonthlyUpdate(config.monthly_updates, channelKey, year, month))
		{
			return 1;
		}
		if (WidgetRules.HasMonthlyMakeup(config.monthly_updates, channelKey, year, month))
		{
			return -3;
		}
		if (month == currentMonth)
		{
			return -2;
		}
		return historyComplete ? -1 : 0;
	}

	private bool HasCreatorMakeupCandidate(string channelKey, DateTime now)
	{
		if (!IsCreatorHistoryComplete(channelKey, now.Year))
		{
			return false;
		}
		for (int i = 1; i < now.Month; i++)
		{
			if (GetCreatorState(channelKey, now.Year, i, now.Month, historyComplete: true) == -1)
			{
				return true;
			}
		}
		return false;
	}

	private void UseProfessionalMakeupCard()
	{
		List<DateTime> list = new List<DateTime>();
		for (int i = 1; i <= 90; i++)
		{
			DateTime date = DateTime.Now.Date.AddDays(-i);
			if (!ProfessionalCheckinService.IsCheckedIn(config, date))
			{
				list.Add(date);
			}
		}
		DateTime? dateTime = MakeupPickerForm.Pick(this, "选择要补签的专业软件日期", list, monthly: false);
		if (!dateTime.HasValue || MessageBox.Show("消耗 1 张专业补签卡补签 " + dateTime.Value.ToString("M 月 d 日") + "？", AppInfo.DisplayName, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
		{
			return;
		}
		if (ProfessionalCheckinService.TryUseMakeupCard(config, dateTime.Value))
		{
			ConfigStore.Save(config);
			RebuildUi();
		}
	}

	private void UseCreatorMakeupCard()
	{
		string text = config.creator_state?.channel_key ?? "";
		List<DateTime> list = new List<DateTime>();
		for (int i = 1; i < DateTime.Now.Month; i++)
		{
			if (GetCreatorState(text, DateTime.Now.Year, i, DateTime.Now.Month, historyComplete: true) == -1)
			{
				list.Add(new DateTime(DateTime.Now.Year, i, 1));
			}
		}
		DateTime? dateTime = MakeupPickerForm.Pick(this, "选择要补签的投稿月份", list, monthly: true);
		if (!dateTime.HasValue || MessageBox.Show("消耗 1 张投稿补签卡补签 " + dateTime.Value.ToString("yyyy 年 M 月") + "？", AppInfo.DisplayName, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
		{
			return;
		}
		if (CreatorCheckinService.TryUseMakeupCard(config, text, dateTime.Value))
		{
			ConfigStore.Save(config);
			RebuildUi();
		}
	}

	private void RebuildUi()
	{
		while (Controls.Count > 0)
		{
			Control control = Controls[0];
			Controls.RemoveAt(0);
			control.Dispose();
		}
		BuildUi();
	}

	private static Control CreateEmptyState()
	{
		CardPanel card = new CardPanel
		{
			Size = new Size(500, 132),
			Margin = new Padding(0, 18, 0, 0),
			BackColor = Theme.PanelBackground,
			EdgeAccent = Theme.BenchmarkGold
		};
		Label label = new Label
		{
			Text = "暂未检测到专业软件",
			ForeColor = Theme.TextPrimary,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(20, 24)
		};
		card.Controls.Add(label);
		Label label2 = new Label
		{
			Text = "打开软件即可完成打卡；只有专业软件位于前台且最近有人操作时，\nUpLingo 才会在本机累计活跃工作时长。",
			ForeColor = Theme.TextMuted,
			BackColor = Color.Transparent,
			Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular),
			AutoSize = true,
			Location = new Point(20, 54)
		};
		card.Controls.Add(label2);
		return card;
	}

	private static bool IsToday(UsageStatConfig record)
	{
		return string.Equals(record.today_date, DateTime.Now.ToString("yyyy-MM-dd"), StringComparison.Ordinal);
	}

	private static string FormatLastUsed(string value)
	{
		if (!DateTime.TryParse(value, out var result))
		{
			return "最近使用时间未知";
		}
		if (result.Date == DateTime.Now.Date)
		{
			return "最后使用 今天 " + result.ToString("HH:mm");
		}
		return "最后使用 " + result.ToString("MM-dd HH:mm");
	}
}
