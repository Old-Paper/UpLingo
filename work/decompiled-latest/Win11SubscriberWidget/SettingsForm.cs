using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal class SettingsForm : Form
{
	private TextBox benchBiliUidBox;

	private TextBox benchYoutubeChannelBox;

	private TextBox youtubeKeyBox;

	private NumericUpDown overtakeWarnBox;

	private NumericUpDown surgeAlertBox;

	private ComboBox refreshSecondsBox;

	private CheckBox fullCountsBox;

	private CheckBox trayDataBox;

	private CheckBox dockToTrayBox;

	private ComboBox closeActionBox;

	private CheckBox startupBox;

	private WidgetConfig config;

	private Label saveStatusLabel;

	public event EventHandler Applied;

	public SettingsForm(WidgetConfig editConfig)
	{
		config = editConfig;
		config.ApplyDefaults();
		Text = AppInfo.DisplayName + " · 设置";
		Icon = AppIcon.Load();
		base.StartPosition = FormStartPosition.CenterParent;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MinimizeBox = false;
		base.MaximizeBox = false;
		base.ShowInTaskbar = false;
		base.AutoScaleMode = AutoScaleMode.Dpi;
		base.AutoScroll = true;
		base.ClientSize = new Size(470, 574);
		BackColor = Theme.PanelBackground;
		ForeColor = Theme.TextPrimary;
		Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
		BuildUi();
		LoadValues();
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		NativeMethods.ApplyDarkTitleBar(base.Handle);
		NativeMethods.ApplyRoundedCorners(base.Handle);
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		NativeMethods.SetTextBoxPlaceholder(benchBiliUidBox, "参考 UID，多个用逗号分隔");
		NativeMethods.SetTextBoxPlaceholder(benchYoutubeChannelBox, "参考频道，多个用逗号分隔");
		NativeMethods.SetTextBoxPlaceholder(youtubeKeyBox, "Google Cloud 的 Data API v3 密钥");
		Rectangle workingArea = Screen.FromControl(this).WorkingArea;
		if (Height > workingArea.Height - 48)
		{
			Height = Math.Max(420, workingArea.Height - 48);
		}
	}

	private void BuildUi()
	{
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.Padding = new Padding(20, 8, 20, 14);
		tableLayoutPanel.BackColor = Theme.PanelBackground;
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.RowCount = 16;
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		base.Controls.Add(tableLayoutPanel);
		benchBiliUidBox = CreateTextBox();
		benchYoutubeChannelBox = CreateTextBox();
		youtubeKeyBox = CreateTextBox();
		refreshSecondsBox = CreateComboBox();
		refreshSecondsBox.Items.Add(new RefreshOption(60, "60 秒"));
		refreshSecondsBox.Items.Add(new RefreshOption(300, "5 分钟"));
		refreshSecondsBox.Items.Add(new RefreshOption(900, "15 分钟"));
		refreshSecondsBox.Items.Add(new RefreshOption(3600, "60 分钟"));
		overtakeWarnBox = new NumericUpDown();
		overtakeWarnBox.Minimum = 1m;
		overtakeWarnBox.Maximum = 50m;
		overtakeWarnBox.Width = 120;
		overtakeWarnBox.BackColor = Theme.InputBackground;
		overtakeWarnBox.ForeColor = Theme.TextPrimary;
		overtakeWarnBox.BorderStyle = BorderStyle.FixedSingle;
		surgeAlertBox = new NumericUpDown();
		surgeAlertBox.Minimum = 1m;
		surgeAlertBox.Maximum = 100m;
		surgeAlertBox.Width = 120;
		surgeAlertBox.BackColor = Theme.InputBackground;
		surgeAlertBox.ForeColor = Theme.TextPrimary;
		surgeAlertBox.BorderStyle = BorderStyle.FixedSingle;
		fullCountsBox = CreateCheckBox("以完整数字显示，如 123,456");
		trayDataBox = CreateCheckBox("托盘图标轮播 B站 / YouTube 粉丝数（关闭时固定 YT）");
		dockToTrayBox = CreateCheckBox("面板停靠在屏幕右下角");
		closeActionBox = CreateComboBox();
		closeActionBox.Items.Add("最小化到系统托盘");
		closeActionBox.Items.Add("退出整个软件");
		startupBox = CreateCheckBox("登录 Windows 时自动运行");
		int row = 0;
		AddSectionHeader(tableLayoutPanel, row++, "参考频道");
		AddRow(tableLayoutPanel, row++, "参考 B 站 UID", benchBiliUidBox);
		AddRow(tableLayoutPanel, row++, "参考 YouTube", benchYoutubeChannelBox);
		AddRow(tableLayoutPanel, row++, "接近提醒（%）", overtakeWarnBox);
		AddRow(tableLayoutPanel, row++, "异动提醒（%）", surgeAlertBox);
		AddSectionHeader(tableLayoutPanel, row++, "数据与刷新");
		AddRow(tableLayoutPanel, row++, "YouTube API key", youtubeKeyBox);
		AddRow(tableLayoutPanel, row++, "刷新间隔", refreshSecondsBox);
		AddRow(tableLayoutPanel, row++, "完整数字", fullCountsBox);
		AddRow(tableLayoutPanel, row++, "托盘轮播", trayDataBox);
		AddSectionHeader(tableLayoutPanel, row++, "窗口行为");
		AddRow(tableLayoutPanel, row++, "停靠位置", dockToTrayBox);
		AddRow(tableLayoutPanel, row++, "关闭小组件时", closeActionBox);
		AddRow(tableLayoutPanel, row++, "开机启动", startupBox);
		Label versionLabel = new Label
		{
			Text = AppInfo.DisplayName,
			ForeColor = Theme.TextMuted,
			AutoSize = true,
			Margin = new Padding(8, 12, 0, 0)
		};
		tableLayoutPanel.Controls.Add(versionLabel, 0, row);
		tableLayoutPanel.SetColumnSpan(versionLabel, 2);
		row++;
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.BackColor = Theme.PanelBackground;
		flowLayoutPanel.Margin = new Padding(0, 10, 0, 0);
		Button button = CreateButton("应用", primary: true);
		button.Click += delegate
		{
			SaveAndApply();
		};
		Button button2 = CreateButton("关闭", primary: false);
		button2.Click += delegate
		{
			Close();
		};
		saveStatusLabel = new Label
		{
			Text = "",
			ForeColor = Theme.Success,
			AutoSize = true,
			Margin = new Padding(0, 8, 12, 0)
		};
		flowLayoutPanel.Controls.Add(button);
		flowLayoutPanel.Controls.Add(button2);
		flowLayoutPanel.Controls.Add(saveStatusLabel);
		tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, row);
		tableLayoutPanel.SetColumnSpan(flowLayoutPanel, 2);
	}

	private static TextBox CreateTextBox()
	{
		return new TextBox
		{
			BackColor = Theme.InputBackground,
			ForeColor = Theme.TextPrimary,
			BorderStyle = BorderStyle.FixedSingle
		};
	}

	private static CheckBox CreateCheckBox(string text)
	{
		return new CheckBox
		{
			Text = text,
			AutoSize = true,
			ForeColor = Theme.TextSecondary
		};
	}

	private static ComboBox CreateComboBox()
	{
		return new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			BackColor = Theme.InputBackground,
			ForeColor = Theme.TextPrimary,
			FlatStyle = FlatStyle.Flat
		};
	}

	private static Button CreateButton(string text, bool primary)
	{
		Button button = new Button();
		button.Text = text;
		button.Width = 92;
		button.Height = 32;
		button.FlatStyle = FlatStyle.Flat;
		button.Cursor = Cursors.Hand;
		if (primary)
		{
			button.BackColor = Theme.BiliAccent;
			button.ForeColor = Color.White;
			button.FlatAppearance.BorderSize = 0;
			button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 185, 244);
		}
		else
		{
			button.BackColor = Theme.InputBackground;
			button.ForeColor = Theme.TextSecondary;
			button.FlatAppearance.BorderColor = Theme.CardBorder;
			button.FlatAppearance.MouseOverBackColor = Theme.TrackBackground;
		}
		return button;
	}

	private static void AddSectionHeader(TableLayoutPanel table, int row, string text)
	{
		Label label = new Label();
		label.Text = text;
		label.ForeColor = Theme.BenchmarkGold;
		label.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
		label.AutoSize = true;
		label.Margin = new Padding(0, 14, 0, 4);
		table.Controls.Add(label, 0, row);
		table.SetColumnSpan(label, 2);
	}

	private static void AddRow(TableLayoutPanel table, int row, string labelText, Control input)
	{
		Label label = new Label();
		label.Text = labelText;
		label.ForeColor = Theme.TextMuted;
		label.TextAlign = ContentAlignment.MiddleLeft;
		label.Dock = DockStyle.Fill;
		input.Dock = DockStyle.Fill;
		input.Margin = new Padding(0, 5, 0, 5);
		label.Margin = new Padding(8, 5, 8, 5);
		table.Controls.Add(label, 0, row);
		table.Controls.Add(input, 1, row);
	}

	private void LoadValues()
	{
		benchBiliUidBox.Text = JoinBenchmarkValues("bilibili");
		benchYoutubeChannelBox.Text = JoinBenchmarkValues("youtube");
		youtubeKeyBox.Text = config.youtube_api_key;
		overtakeWarnBox.Value = Math.Max(1, Math.Min(50, (config.overtake_warn_percent <= 0) ? 10 : config.overtake_warn_percent));
		surgeAlertBox.Value = Math.Max(1, Math.Min(100, (config.surge_alert_percent <= 0) ? 10 : config.surge_alert_percent));
		SelectRefreshSeconds(config.refresh_seconds);
		fullCountsBox.Checked = config.show_full_counts;
		trayDataBox.Checked = config.show_tray_counts;
		dockToTrayBox.Checked = config.dock_to_tray;
		closeActionBox.SelectedIndex = string.Equals(config.close_action, WidgetCloseActions.Exit, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
		startupBox.Checked = StartupManager.IsEnabled();
	}

	private void SaveAndApply()
	{
		ApplyBenchmarkChannels("bilibili", "B站参考", benchBiliUidBox.Text);
		ApplyBenchmarkChannels("youtube", "YouTube参考", benchYoutubeChannelBox.Text);
		config.youtube_api_key = youtubeKeyBox.Text.Trim();
		config.overtake_warn_percent = Convert.ToInt32(overtakeWarnBox.Value);
		config.surge_alert_percent = Convert.ToInt32(surgeAlertBox.Value);
		config.refresh_seconds = SelectedRefreshSeconds();
		config.refresh_minutes = Math.Max(1, config.refresh_seconds / 60);
		config.low_power_mode = false;
		config.show_full_counts = fullCountsBox.Checked;
		config.show_tray_counts = trayDataBox.Checked;
		config.dock_to_tray = dockToTrayBox.Checked;
		config.close_action = (closeActionBox.SelectedIndex == 1) ? WidgetCloseActions.Exit : WidgetCloseActions.Tray;
		config.silent_start = false;
		try
		{
			StartupManager.SetEnabled(startupBox.Checked);
			ConfigStore.Save(config);
			if (!ConfigStore.TryReadCurrent(out WidgetConfig saved) || saved.refresh_seconds != config.refresh_seconds || !string.Equals(saved.close_action, config.close_action, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("保存后的配置校验失败");
			}
			saveStatusLabel.ForeColor = Theme.Success;
			saveStatusLabel.Text = "已保存";
			Applied?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			saveStatusLabel.ForeColor = Theme.Error;
			saveStatusLabel.Text = "保存失败";
			AppLogger.Error("settings-save", ex);
			MessageBox.Show("设置未能保存：" + ex.Message, AppInfo.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}

	private void SelectRefreshSeconds(int seconds)
	{
		int normalized = WidgetConfig.NormalizeRefreshSeconds(seconds);
		for (int i = 0; i < refreshSecondsBox.Items.Count; i++)
		{
			if (refreshSecondsBox.Items[i] is RefreshOption option && option.Seconds == normalized)
			{
				refreshSecondsBox.SelectedIndex = i;
				return;
			}
		}
		refreshSecondsBox.SelectedIndex = refreshSecondsBox.Items.Count - 1;
	}

	private int SelectedRefreshSeconds()
	{
		return (refreshSecondsBox.SelectedItem as RefreshOption)?.Seconds ?? 3600;
	}

	private ChannelConfig GetOrCreateChannel(string platform, string label)
	{
		for (int i = 0; i < config.channels.Count; i++)
		{
			if (!config.channels[i].benchmark && PlatformsEqual(config.channels[i].platform, platform))
			{
				return config.channels[i];
			}
		}
		ChannelConfig channelConfig = new ChannelConfig();
		channelConfig.platform = platform;
		channelConfig.label = label;
		config.channels.Add(channelConfig);
		return channelConfig;
	}

	private List<ChannelConfig> FindBenchmarkChannels(string platform)
	{
		List<ChannelConfig> list = new List<ChannelConfig>();
		for (int i = 0; i < config.channels.Count; i++)
		{
			if (config.channels[i].benchmark && PlatformsEqual(config.channels[i].platform, platform))
			{
				list.Add(config.channels[i]);
			}
		}
		return list;
	}

	private static string BenchmarkValue(ChannelConfig channel, string platform)
	{
		if (PlatformsEqual(platform, "bilibili"))
		{
			return First(channel.bilibili_uid, channel.uid, channel.vmid);
		}
		return First(channel.youtube_channel, channel.youtube_channel_id, channel.channel_id, channel.youtube_handle, channel.handle, channel.youtube_url, channel.url);
	}

	private string JoinBenchmarkValues(string platform)
	{
		List<string> list = new List<string>();
		foreach (ChannelConfig item in FindBenchmarkChannels(platform))
		{
			string text = BenchmarkValue(item, platform);
			if (text.Length > 0)
			{
				list.Add(text);
			}
		}
		return string.Join(", ", list.ToArray());
	}

	private void ApplyBenchmarkChannels(string platform, string labelPrefix, string inputText)
	{
		string[] array = (inputText ?? "").Split(new char[6] { ',', '，', ';', '；', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text = array2[i].Trim();
			if (text.Length > 0 && hashSet.Add(text))
			{
				list.Add(text);
			}
		}
		List<ChannelConfig> list2 = FindBenchmarkChannels(platform);
		ChannelConfig targetChannel = GetOrCreateChannel(platform, PlatformsEqual(platform, "bilibili") ? "B站频道" : "YouTube频道");
		foreach (ChannelConfig item in list2)
		{
			if (!list.Contains(BenchmarkValue(item, platform)))
			{
				config.channels.Remove(item);
			}
		}
		list2 = FindBenchmarkChannels(platform);
		foreach (string item2 in list)
		{
			bool flag = false;
			foreach (ChannelConfig item3 in list2)
			{
				if (string.Equals(BenchmarkValue(item3, platform), item2, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				ChannelConfig channelConfig = new ChannelConfig();
				channelConfig.platform = platform;
				channelConfig.benchmark = true;
				channelConfig.compare_to_key = ChannelIdentity.CacheKey(targetChannel);
				channelConfig.label = ((list.Count == 1) ? labelPrefix : (labelPrefix + (list2.Count + 1)));
				if (PlatformsEqual(platform, "bilibili"))
				{
					channelConfig.bilibili_uid = item2;
				}
				else
				{
					channelConfig.youtube_channel = item2;
				}
				config.channels.Add(channelConfig);
				list2.Add(channelConfig);
			}
		}
		foreach (ChannelConfig benchmark in list2)
		{
			benchmark.compare_to_key = ChannelIdentity.CacheKey(targetChannel);
		}
	}

	private static bool PlatformsEqual(string a, string b)
	{
		return NormalizePlatformName(a) == NormalizePlatformName(b);
	}

	private static string NormalizePlatformName(string platform)
	{
		string text = (platform ?? "").Trim().ToLowerInvariant();
		switch (text)
		{
		case "bili":
		case "b站":
			return "bilibili";
		case "yt":
		case "油管":
			return "youtube";
		default:
			return text;
		}
	}

	private static string First(params string[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (!string.IsNullOrEmpty(values[i]))
			{
				return values[i].Trim();
			}
		}
		return "";
	}

	private sealed class RefreshOption
	{
		public int Seconds { get; }

		private string Text { get; }

		public RefreshOption(int seconds, string text)
		{
			Seconds = seconds;
			Text = text;
		}

		public override string ToString()
		{
			return Text;
		}
	}
}
