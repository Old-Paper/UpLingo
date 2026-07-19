using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal class SettingsForm : Form
{
	private TextBox biliUidBox;

	private TextBox youtubeChannelBox;

	private TextBox benchBiliUidBox;

	private TextBox benchYoutubeChannelBox;

	private TextBox youtubeKeyBox;

	private NumericUpDown overtakeWarnBox;

	private NumericUpDown surgeAlertBox;

	private NumericUpDown refreshMinutesBox;

	private CheckBox lowPowerBox;

	private CheckBox fullCountsBox;

	private CheckBox trayDataBox;

	private CheckBox dockToTrayBox;

	private CheckBox topmostBox;

	private CheckBox lockPositionBox;

	private CheckBox silentStartBox;

	private CheckBox startupBox;

	private WidgetConfig config;

	public WidgetConfig ResultConfig { get; private set; }

	public SettingsForm(WidgetConfig editConfig)
	{
		config = editConfig;
		config.ApplyDefaults();
		Text = AppInfo.DisplayName + " · 设置";
		base.StartPosition = FormStartPosition.CenterParent;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MinimizeBox = false;
		base.MaximizeBox = false;
		base.ShowInTaskbar = false;
		base.AutoScaleMode = AutoScaleMode.Dpi;
		base.AutoScroll = true;
		base.ClientSize = new Size(470, 736);
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
		NativeMethods.SetTextBoxPlaceholder(biliUidBox, "个人空间链接里的数字");
		NativeMethods.SetTextBoxPlaceholder(youtubeChannelBox, "@handle、频道 ID 或链接");
		NativeMethods.SetTextBoxPlaceholder(benchBiliUidBox, "对标 UID，多个用逗号分隔");
		NativeMethods.SetTextBoxPlaceholder(benchYoutubeChannelBox, "对标频道，多个用逗号分隔");
		NativeMethods.SetTextBoxPlaceholder(youtubeKeyBox, "Google Cloud 的 Data API v3 密钥");
	}

	private void BuildUi()
	{
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.Padding = new Padding(20, 8, 20, 14);
		tableLayoutPanel.BackColor = Theme.PanelBackground;
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.RowCount = 21;
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		base.Controls.Add(tableLayoutPanel);
		biliUidBox = CreateTextBox();
		youtubeChannelBox = CreateTextBox();
		benchBiliUidBox = CreateTextBox();
		benchYoutubeChannelBox = CreateTextBox();
		youtubeKeyBox = CreateTextBox();
		refreshMinutesBox = new NumericUpDown();
		refreshMinutesBox.Minimum = 1m;
		refreshMinutesBox.Maximum = 1440m;
		refreshMinutesBox.Width = 120;
		refreshMinutesBox.BackColor = Theme.InputBackground;
		refreshMinutesBox.ForeColor = Theme.TextPrimary;
		refreshMinutesBox.BorderStyle = BorderStyle.FixedSingle;
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
		lowPowerBox = CreateCheckBox("省电模式，刷新间隔不低于 60 分钟");
		fullCountsBox = CreateCheckBox("以完整数字显示，如 14,000");
		trayDataBox = CreateCheckBox("托盘图标轮播 B站 / YouTube 粉丝数（关闭时固定 YT）");
		dockToTrayBox = CreateCheckBox("面板停靠在屏幕右下角");
		topmostBox = CreateCheckBox("面板始终悬浮在最上层");
		lockPositionBox = CreateCheckBox("锁定位置，禁止拖动面板");
		silentStartBox = CreateCheckBox("启动后直接进托盘，不弹出面板");
		startupBox = CreateCheckBox("登录 Windows 时自动运行");
		int row = 0;
		AddSectionHeader(tableLayoutPanel, row++, "我的频道");
		AddRow(tableLayoutPanel, row++, "B 站 UID", biliUidBox);
		AddRow(tableLayoutPanel, row++, "YouTube 频道", youtubeChannelBox);
		AddSectionHeader(tableLayoutPanel, row++, "对标频道");
		AddRow(tableLayoutPanel, row++, "对标 B 站 UID", benchBiliUidBox);
		AddRow(tableLayoutPanel, row++, "对标 YouTube", benchYoutubeChannelBox);
		AddRow(tableLayoutPanel, row++, "反超预警（%）", overtakeWarnBox);
		AddRow(tableLayoutPanel, row++, "异动提醒（%）", surgeAlertBox);
		AddSectionHeader(tableLayoutPanel, row++, "数据与刷新");
		AddRow(tableLayoutPanel, row++, "YouTube API key", youtubeKeyBox);
		AddRow(tableLayoutPanel, row++, "刷新间隔（分钟）", refreshMinutesBox);
		AddRow(tableLayoutPanel, row++, "省电模式", lowPowerBox);
		AddRow(tableLayoutPanel, row++, "完整数字", fullCountsBox);
		AddRow(tableLayoutPanel, row++, "托盘轮播", trayDataBox);
		AddSectionHeader(tableLayoutPanel, row++, "窗口行为");
		AddRow(tableLayoutPanel, row++, "停靠位置", dockToTrayBox);
		AddRow(tableLayoutPanel, row++, "窗口置顶", topmostBox);
		AddRow(tableLayoutPanel, row++, "锁定位置", lockPositionBox);
		AddRow(tableLayoutPanel, row++, "静默启动", silentStartBox);
		AddRow(tableLayoutPanel, row++, "开机启动", startupBox);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.BackColor = Theme.PanelBackground;
		flowLayoutPanel.Margin = new Padding(0, 10, 0, 0);
		Button button = CreateButton("保存", primary: true);
		button.Click += delegate
		{
			SaveAndClose();
		};
		Button button2 = CreateButton("取消", primary: false);
		button2.Click += delegate
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
		};
		flowLayoutPanel.Controls.Add(button);
		flowLayoutPanel.Controls.Add(button2);
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
		ChannelConfig orCreateChannel = GetOrCreateChannel("bilibili", "B站频道");
		ChannelConfig orCreateChannel2 = GetOrCreateChannel("youtube", "YouTube频道");
		biliUidBox.Text = First(orCreateChannel.bilibili_uid, orCreateChannel.uid, orCreateChannel.vmid);
		youtubeChannelBox.Text = First(orCreateChannel2.youtube_channel, orCreateChannel2.youtube_channel_id, orCreateChannel2.channel_id, orCreateChannel2.youtube_handle, orCreateChannel2.handle, orCreateChannel2.youtube_url, orCreateChannel2.url);
		benchBiliUidBox.Text = JoinBenchmarkValues("bilibili");
		benchYoutubeChannelBox.Text = JoinBenchmarkValues("youtube");
		youtubeKeyBox.Text = config.youtube_api_key;
		overtakeWarnBox.Value = Math.Max(1, Math.Min(50, (config.overtake_warn_percent <= 0) ? 10 : config.overtake_warn_percent));
		surgeAlertBox.Value = Math.Max(1, Math.Min(100, (config.surge_alert_percent <= 0) ? 10 : config.surge_alert_percent));
		refreshMinutesBox.Value = Math.Max(1, Math.Min(1440, (config.refresh_minutes <= 0) ? 60 : config.refresh_minutes));
		lowPowerBox.Checked = config.low_power_mode;
		fullCountsBox.Checked = config.show_full_counts;
		trayDataBox.Checked = config.show_tray_counts;
		dockToTrayBox.Checked = config.dock_to_tray;
		topmostBox.Checked = config.always_on_top;
		lockPositionBox.Checked = config.lock_position;
		silentStartBox.Checked = config.silent_start;
		startupBox.Checked = StartupManager.IsEnabled();
	}

	private void SaveAndClose()
	{
		ChannelConfig orCreateChannel = GetOrCreateChannel("bilibili", "B站频道");
		ChannelConfig orCreateChannel2 = GetOrCreateChannel("youtube", "YouTube频道");
		orCreateChannel.platform = "bilibili";
		orCreateChannel.label = (string.IsNullOrEmpty(orCreateChannel.label) ? "B站频道" : orCreateChannel.label);
		orCreateChannel.bilibili_uid = biliUidBox.Text.Trim();
		orCreateChannel.uid = null;
		orCreateChannel.vmid = null;
		orCreateChannel2.platform = "youtube";
		orCreateChannel2.label = (string.IsNullOrEmpty(orCreateChannel2.label) ? "YouTube频道" : orCreateChannel2.label);
		string text = youtubeChannelBox.Text.Trim();
		bool flag = !string.Equals(ChannelIdentity.ConfiguredKey(orCreateChannel2), text, StringComparison.OrdinalIgnoreCase);
		orCreateChannel2.youtube_channel = text;
		if (flag)
		{
			orCreateChannel2.youtube_channel_id = null;
			orCreateChannel2.channel_id = null;
		}
		orCreateChannel2.youtube_handle = null;
		orCreateChannel2.handle = null;
		orCreateChannel2.youtube_username = null;
		orCreateChannel2.username = null;
		orCreateChannel2.youtube_url = null;
		orCreateChannel2.url = null;
		ApplyBenchmarkChannels("bilibili", "B站对标", benchBiliUidBox.Text);
		ApplyBenchmarkChannels("youtube", "YouTube对标", benchYoutubeChannelBox.Text);
		config.youtube_api_key = youtubeKeyBox.Text.Trim();
		config.overtake_warn_percent = Convert.ToInt32(overtakeWarnBox.Value);
		config.surge_alert_percent = Convert.ToInt32(surgeAlertBox.Value);
		config.refresh_minutes = Convert.ToInt32(refreshMinutesBox.Value);
		config.low_power_mode = lowPowerBox.Checked;
		config.show_full_counts = fullCountsBox.Checked;
		config.show_tray_counts = trayDataBox.Checked;
		config.dock_to_tray = dockToTrayBox.Checked;
		config.always_on_top = topmostBox.Checked;
		config.lock_position = lockPositionBox.Checked;
		config.silent_start = silentStartBox.Checked;
		StartupManager.SetEnabled(startupBox.Checked);
		ResultConfig = config;
		base.DialogResult = DialogResult.OK;
		Close();
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
}
