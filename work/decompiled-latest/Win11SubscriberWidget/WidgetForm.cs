using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Threading;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal partial class WidgetForm : Form
{
	private const int StartupRefreshDelayMinutes = 5;

	private const int RowHeight = 70;

	private const int RowSpacing = 8;

	private const int CreatorCardHeight = 54;

	private readonly List<ChannelRow> rows = new List<ChannelRow>();

	private readonly Dictionary<int, List<int>> benchmarksOfTarget = new Dictionary<int, List<int>>();

	private readonly Dictionary<int, int> targetOfBenchmark = new Dictionary<int, int>();

	private WidgetConfig config;

	private NotifyIcon trayIcon;

	private ContextMenuStrip trayMenu;

	private ToolStripMenuItem showHideItem;

	private ToolStripMenuItem lowPowerItem;

	private ToolStripMenuItem fullCountsItem;

	private ToolStripMenuItem trayDataItem;

	private ToolStripMenuItem topmostItem;

	private ToolStripMenuItem lockPositionItem;

	private ToolStripMenuItem silentStartItem;

	private ToolStripMenuItem startupItem;

	private ToolStripMenuItem usageStatsItem;

	private System.Windows.Forms.Timer trayIconTimer;

	private readonly List<TrayMetric> trayMetrics = new List<TrayMetric>();

	private TrayMetric staticYouTubeTrayMetric;

	private List<FetchResult> lastResults = new List<FetchResult>();

	private int trayMetricIndex;

	private Icon generatedTrayIcon;

	private Label statusLabel;

	private Label statusDot;

	private Label sloganLabel;

	private Panel contentPanel;

	private CardPanel creatorCard;

	private Label creatorDot;

	private Label creatorLabel;

	private SegmentedTextLabel creatorSubLabel;

	private DateTime? latestVideoAt;

	private CreatorFetchData lastCreatorFetch;

	private int weeklyMetricMode;

	private System.Windows.Forms.Timer refreshTimer;

	private System.Windows.Forms.Timer usageStatsTimer;

	private DateTime usageStatsLastCaptureUtc;

	private DateTime usageStatsLastSaveUtc;

	private int usageDetectionRunning;

	private readonly ToolTip rowToolTip = new ToolTip
	{
		AutoPopDelay = 12000,
		InitialDelay = 400,
		ReshowDelay = 200
	};

	private bool refreshing;

	private int refreshGeneration;

	private bool appQuitting;

	private bool dragging;

	private bool hideAfterInitialShow;

	private int pendingAchievementCelebrations;

	private Point dragStartScreen;

	private Point dragStartLocation;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams obj = base.CreateParams;
			obj.ClassStyle |= 131072;
			return obj;
		}
	}

	public WidgetForm(WidgetConfig initialConfig, bool autoRefresh)
	{
		config = initialConfig;
		config.ApplyDefaults();
		hideAfterInitialShow = config.silent_start;
		Text = AppInfo.DisplayName;
		base.ShowInTaskbar = false;
		base.FormBorderStyle = FormBorderStyle.None;
		base.StartPosition = FormStartPosition.Manual;
		base.AutoScaleMode = AutoScaleMode.Dpi;
		BackColor = Theme.WindowBackground;
		base.ClientSize = CalculateWidgetSize();
		MinimumSize = new Size(340, 140);
		DoubleBuffered = true;
		RestoreCreatorState();
		BuildUi();
		BuildTrayMenu();
		BuildTrayIcon();
		MoveToSavedPosition();
		ApplyNormalWindowMode();
		refreshTimer = new System.Windows.Forms.Timer();
		refreshTimer.Tick += delegate
		{
			RefreshNow();
		};
		StartUsageStatsTracking();
		if (autoRefresh)
		{
			RenderCachedCountsForStartup();
			ScheduleRefreshAfterMinutes(5);
		}
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		NativeMethods.ApplyRoundedCorners(base.Handle);
	}

	public void QuitFromApp()
	{
		CaptureUsageStats(forceSave: true);
		StopUsageStatsTracking();
		appQuitting = true;
		refreshGeneration++;
		if (trayIconTimer != null)
		{
			trayIconTimer.Stop();
			trayIconTimer.Dispose();
			trayIconTimer = null;
		}
		if (trayIcon != null)
		{
			trayIcon.Visible = false;
			trayIcon.Dispose();
		}
		if (generatedTrayIcon != null)
		{
			generatedTrayIcon.Dispose();
			generatedTrayIcon = null;
		}
		Close();
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if (!appQuitting)
		{
			e.Cancel = true;
			Hide();
		}
		else
		{
			base.OnFormClosing(e);
		}
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		if (pendingAchievementCelebrations > 0)
		{
			FireworksForm.ShowCelebration(this, pendingAchievementCelebrations);
			pendingAchievementCelebrations = 0;
		}
		if (hideAfterInitialShow)
		{
			hideAfterInitialShow = false;
			BeginInvoke((Action)delegate
			{
				Hide();
			});
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		using Pen pen = new Pen(Theme.WindowBorder);
		e.Graphics.DrawRectangle(pen, 0, 0, base.Width - 1, base.Height - 1);
		Rectangle rect = new Rectangle(1, 1, base.Width - 2, 2);
		using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(rect, Theme.BiliAccent, Theme.YouTubeAccent, LinearGradientMode.Horizontal);
		ColorBlend colorBlend = new ColorBlend(3);
		colorBlend.Colors = new Color[3]
		{
			Theme.BiliAccent,
			Theme.BenchmarkGold,
			Theme.YouTubeAccent
		};
		colorBlend.Positions = new float[3] { 0f, 0.5f, 1f };
		linearGradientBrush.InterpolationColors = colorBlend;
		e.Graphics.FillRectangle(linearGradientBrush, rect);
	}

	private int VisibleRowCount()
	{
		int num = 0;
		for (int i = 0; i < config.channels.Count; i++)
		{
			if (!config.channels[i].benchmark)
			{
				num++;
			}
		}
		return num;
	}

	private bool HasOwnYouTubeChannel()
	{
		for (int i = 0; i < config.channels.Count; i++)
		{
			if (!config.channels[i].benchmark && !IsBilibili(config.channels[i].platform))
			{
				return true;
			}
		}
		return false;
	}

	private Size CalculateWidgetSize()
	{
		int num = Math.Max(1, VisibleRowCount());
		int num2 = 76 + num * 78;
		if (HasOwnYouTubeChannel())
		{
			num2 += 62;
		}
		return new Size(360, num2);
	}

	private void BuildUi()
	{
		rowToolTip.RemoveAll();
		while (base.Controls.Count > 0)
		{
			Control control = base.Controls[0];
			base.Controls.RemoveAt(0);
			control.Dispose();
		}
		rows.Clear();
		BuildBenchmarkPairs();
		base.ClientSize = CalculateWidgetSize();
		contentPanel = new Panel();
		contentPanel.BackColor = Theme.PanelBackground;
		contentPanel.Dock = DockStyle.Fill;
		contentPanel.Padding = new Padding(14, 10, 14, 12);
		base.Controls.Add(contentPanel);
		Panel headerPanel = new Panel();
		headerPanel.BackColor = contentPanel.BackColor;
		headerPanel.Dock = DockStyle.Top;
		headerPanel.Height = 26;
		contentPanel.Controls.Add(headerPanel);
		Label label = new Label();
		label.Text = AppInfo.ProductName;
		label.ForeColor = Theme.TextPrimary;
		label.BackColor = headerPanel.BackColor;
		label.Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold);
		label.AutoSize = true;
		label.Location = new Point(0, 2);
		headerPanel.Controls.Add(label);
		rowToolTip.SetToolTip(label, AppInfo.DisplayName);
		statusDot = new Label();
		statusDot.Text = "●";
		statusDot.ForeColor = Theme.TextMuted;
		statusDot.BackColor = headerPanel.BackColor;
		statusDot.Font = new Font("Microsoft YaHei UI", 7f, FontStyle.Regular);
		statusDot.Size = new Size(14, 16);
		statusDot.TextAlign = ContentAlignment.MiddleLeft;
		statusDot.Location = new Point(58, 5);
		headerPanel.Controls.Add(statusDot);
		sloganLabel = new Label();
		sloganLabel.Text = "";
		sloganLabel.ForeColor = Theme.TextSecondary;
		sloganLabel.BackColor = headerPanel.BackColor;
		sloganLabel.Font = new Font("Microsoft YaHei UI", 7.5f, FontStyle.Regular);
		sloganLabel.AutoEllipsis = true;
		sloganLabel.TextAlign = ContentAlignment.MiddleLeft;
		sloganLabel.Cursor = Cursors.Hand;
		sloganLabel.Size = new Size(140, 20);
		sloganLabel.Location = new Point(76, 2);
		sloganLabel.DoubleClick += delegate
		{
			OpenSloganFile();
		};
		headerPanel.Controls.Add(sloganLabel);
		UpdateSlogan();
		Label refreshButton = new Label();
		refreshButton.Text = "↻";
		refreshButton.ForeColor = Theme.TextMuted;
		refreshButton.BackColor = headerPanel.BackColor;
		refreshButton.Font = new Font("Segoe UI Symbol", 11f, FontStyle.Regular);
		refreshButton.TextAlign = ContentAlignment.MiddleCenter;
		refreshButton.Cursor = Cursors.Hand;
		refreshButton.Size = new Size(26, 24);
		refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		refreshButton.Location = new Point(base.ClientSize.Width - 138, 0);
		refreshButton.MouseEnter += delegate
		{
			refreshButton.BackColor = Theme.CardBackground;
			refreshButton.ForeColor = Theme.TextPrimary;
		};
		refreshButton.MouseLeave += delegate
		{
			refreshButton.BackColor = headerPanel.BackColor;
			refreshButton.ForeColor = Theme.TextMuted;
		};
		refreshButton.Click += delegate
		{
			RefreshNow();
		};
		rowToolTip.SetToolTip(refreshButton, "立即刷新");
		headerPanel.Controls.Add(refreshButton);
		Label settingsButton = new Label();
		settingsButton.Text = "⚙";
		settingsButton.ForeColor = Theme.TextMuted;
		settingsButton.BackColor = headerPanel.BackColor;
		settingsButton.Font = new Font("Segoe UI Symbol", 10f, FontStyle.Regular);
		settingsButton.TextAlign = ContentAlignment.MiddleCenter;
		settingsButton.Cursor = Cursors.Hand;
		settingsButton.Size = new Size(26, 24);
		settingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		settingsButton.Location = new Point(base.ClientSize.Width - 82, 0);
		settingsButton.MouseEnter += delegate
		{
			settingsButton.BackColor = Theme.CardBackground;
			settingsButton.ForeColor = Theme.TextPrimary;
		};
		settingsButton.MouseLeave += delegate
		{
			settingsButton.BackColor = headerPanel.BackColor;
			settingsButton.ForeColor = Theme.TextMuted;
		};
		settingsButton.Click += delegate
		{
			OpenSettings();
		};
		headerPanel.Controls.Add(settingsButton);
		Label usageStatsButton = new Label();
		usageStatsButton.Text = "◷";
		usageStatsButton.ForeColor = Theme.TextMuted;
		usageStatsButton.BackColor = headerPanel.BackColor;
		usageStatsButton.Font = new Font("Segoe UI Symbol", 10.5f, FontStyle.Regular);
		usageStatsButton.TextAlign = ContentAlignment.MiddleCenter;
		usageStatsButton.Cursor = Cursors.Hand;
		usageStatsButton.Size = new Size(26, 24);
		usageStatsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		usageStatsButton.Location = new Point(base.ClientSize.Width - 110, 0);
		usageStatsButton.MouseEnter += delegate
		{
			usageStatsButton.BackColor = Theme.CardBackground;
			usageStatsButton.ForeColor = Theme.TextPrimary;
		};
		usageStatsButton.MouseLeave += delegate
		{
			usageStatsButton.BackColor = headerPanel.BackColor;
			usageStatsButton.ForeColor = Theme.TextMuted;
		};
		usageStatsButton.Click += delegate
		{
			OpenUsageStats();
		};
		rowToolTip.SetToolTip(usageStatsButton, "使用统计");
		headerPanel.Controls.Add(usageStatsButton);
		Label closeButton = new Label();
		closeButton.Text = "×";
		closeButton.ForeColor = Theme.TextMuted;
		closeButton.BackColor = headerPanel.BackColor;
		closeButton.Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Regular);
		closeButton.TextAlign = ContentAlignment.MiddleCenter;
		closeButton.Cursor = Cursors.Hand;
		closeButton.Size = new Size(26, 24);
		closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		closeButton.Location = new Point(base.ClientSize.Width - 54, 0);
		closeButton.MouseEnter += delegate
		{
			closeButton.BackColor = Theme.CloseHoverBackground;
			closeButton.ForeColor = Theme.TextPrimary;
		};
		closeButton.MouseLeave += delegate
		{
			closeButton.BackColor = headerPanel.BackColor;
			closeButton.ForeColor = Theme.TextMuted;
		};
		closeButton.Click += delegate
		{
			Hide();
		};
		headerPanel.Controls.Add(closeButton);
		statusLabel = new Label();
		statusLabel.Text = "等待刷新";
		statusLabel.ForeColor = Theme.TextMuted;
		statusLabel.BackColor = contentPanel.BackColor;
		statusLabel.Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Regular);
		statusLabel.TextAlign = ContentAlignment.MiddleLeft;
		statusLabel.Dock = DockStyle.Top;
		statusLabel.Height = 22;
		contentPanel.Controls.Add(statusLabel);
		statusLabel.BringToFront();
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.WrapContents = false;
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.BackColor = contentPanel.BackColor;
		flowLayoutPanel.Padding = new Padding(0, 2, 0, 0);
		contentPanel.Controls.Add(flowLayoutPanel);
		flowLayoutPanel.BringToFront();
		if (VisibleRowCount() == 0)
		{
			Label label2 = new Label();
			label2.Text = "还没有频道，右键打开设置添加";
			label2.ForeColor = Theme.TextMuted;
			label2.BackColor = contentPanel.BackColor;
			label2.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
			label2.AutoSize = true;
			label2.Margin = new Padding(2, 12, 0, 0);
			flowLayoutPanel.Controls.Add(label2);
		}
		for (int num = 0; num < config.channels.Count; num++)
		{
			if (!config.channels[num].benchmark)
			{
				flowLayoutPanel.Controls.Add(CreateChannelRow(config.channels[num], num, benchmarksOfTarget.ContainsKey(num)));
			}
		}
		if (HasOwnYouTubeChannel())
		{
			flowLayoutPanel.Controls.Add(CreateCreatorCard());
			UpdateCreatorCard();
		}
		else
		{
			creatorCard = null;
			creatorDot = null;
			creatorLabel = null;
		}
		AttachWidgetMouseEvents(this);
		AttachWidgetMouseEvents(contentPanel);
		AttachWidgetMouseEvents(headerPanel);
		AttachWidgetMouseEvents(flowLayoutPanel);
	}

	private void BuildBenchmarkPairs()
	{
		benchmarksOfTarget.Clear();
		targetOfBenchmark.Clear();
		for (int i = 0; i < config.channels.Count; i++)
		{
			if (config.channels[i] == null || !config.channels[i].benchmark)
			{
				continue;
			}
			int num = FindBenchmarkTargetIndex(config.channels[i]);
			if (num >= 0)
			{
				targetOfBenchmark[i] = num;
				if (!benchmarksOfTarget.TryGetValue(num, out var value))
				{
					value = new List<int>();
					benchmarksOfTarget[num] = value;
				}
				value.Add(i);
			}
		}
	}

	private Control CreateChannelRow(ChannelConfig channel, int channelIndex, bool hasBenchmark)
	{
		bool flag = IsBilibili(channel.platform);
		Color color = (flag ? Theme.BiliAccent : Theme.YouTubeAccent);
		CardPanel cardPanel = new CardPanel();
		cardPanel.BackColor = Theme.PanelBackground;
		cardPanel.Size = new Size(base.ClientSize.Width - 28, 70);
		cardPanel.Margin = new Padding(0, 4, 0, 4);
		Label label = new Label();
		label.Text = "-- 进度 --%";
		label.BackColor = Color.Transparent;
		label.ForeColor = Theme.TextMuted;
		label.Font = new Font("Microsoft YaHei UI", 6.8f, FontStyle.Regular);
		label.AutoEllipsis = true;
		label.Size = new Size(88, 13);
		label.Location = new Point(12, 2);
		cardPanel.Controls.Add(label);
		MilestoneBar milestoneBar = new MilestoneBar();
		milestoneBar.AccentColor = color;
		milestoneBar.BackColor = Color.Transparent;
		milestoneBar.Size = new Size(cardPanel.Width - 116, 5);
		milestoneBar.Location = new Point(104, 6);
		cardPanel.Controls.Add(milestoneBar);
		BadgeLabel badgeLabel = new BadgeLabel();
		badgeLabel.Text = (flag ? "B" : "YT");
		badgeLabel.BadgeColor = color;
		badgeLabel.ForeColor = Color.White;
		badgeLabel.BackColor = Color.Transparent;
		badgeLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
		badgeLabel.Size = new Size(28, 28);
		badgeLabel.Location = new Point(12, 21);
		cardPanel.Controls.Add(badgeLabel);
		Label label2 = new Label();
		label2.Text = SafeText(channel.label, channel.platform) + (hasBenchmark ? "  ◆" : "");
		label2.BackColor = Color.Transparent;
		label2.ForeColor = Theme.TextPrimary;
		label2.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
		label2.AutoEllipsis = true;
		label2.Size = new Size(168, 20);
		label2.Location = new Point(50, 15);
		cardPanel.Controls.Add(label2);
		Label label3 = new Label();
		label3.Text = "等待刷新";
		label3.BackColor = Color.Transparent;
		label3.ForeColor = Theme.TextMuted;
		label3.Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Regular);
		label3.AutoEllipsis = true;
		label3.Size = new Size(152, 17);
		label3.Location = new Point(50, 37);
		cardPanel.Controls.Add(label3);
		CountDisplay countDisplay = new CountDisplay();
		countDisplay.SetCount("--");
		countDisplay.BackColor = Color.Transparent;
		countDisplay.ForeColor = Theme.TextPrimary;
		countDisplay.Size = new Size(112, 42);
		countDisplay.Location = new Point(cardPanel.Width - 124, 15);
		cardPanel.Controls.Add(countDisplay);
		DeltaBar deltaBar = null;
		if (hasBenchmark)
		{
			deltaBar = new DeltaBar();
			deltaBar.BackColor = Color.Transparent;
			deltaBar.Size = new Size(cardPanel.Width - 50 - 14, 4);
			deltaBar.Location = new Point(50, 60);
			cardPanel.Controls.Add(deltaBar);
			AttachWidgetMouseEvents(deltaBar);
		}
		AttachRowOpenEvents(cardPanel, channelIndex);
		AttachRowOpenEvents(badgeLabel, channelIndex);
		AttachRowOpenEvents(label2, channelIndex);
		AttachRowOpenEvents(label3, channelIndex);
		AttachRowOpenEvents(countDisplay, channelIndex);
		AttachRowOpenEvents(label, channelIndex);
		AttachRowOpenEvents(milestoneBar, channelIndex);
		rows.Add(new ChannelRow
		{
			ChannelIndex = channelIndex,
			Card = cardPanel,
			CountLabel = countDisplay,
			DetailLabel = label3,
			MilestoneLabel = label,
			MilestoneProgressBar = milestoneBar,
			ProgressBar = deltaBar
		});
		AttachWidgetMouseEvents(cardPanel);
		AttachWidgetMouseEvents(badgeLabel);
		AttachWidgetMouseEvents(label2);
		AttachWidgetMouseEvents(label3);
		AttachWidgetMouseEvents(countDisplay);
		AttachWidgetMouseEvents(label);
		AttachWidgetMouseEvents(milestoneBar);
		return cardPanel;
	}

	private void BuildTrayMenu()
	{
		trayMenu = new ContextMenuStrip();
		showHideItem = new ToolStripMenuItem("隐藏小组件");
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("立即刷新");
		ToolStripMenuItem restorePositionItem = new ToolStripMenuItem("恢复到右下角");
		ToolStripMenuItem toolStripMenuItem2 = new ToolStripMenuItem("设置...");
		ToolStripMenuItem toolStripMenuItem3 = new ToolStripMenuItem("成就记录...");
		usageStatsItem = new ToolStripMenuItem("使用统计...");
		lowPowerItem = new ToolStripMenuItem("省电模式（间隔至少 60 分钟）");
		fullCountsItem = new ToolStripMenuItem("显示完整数字");
		trayDataItem = new ToolStripMenuItem("轮播 B站 / YouTube 粉丝数（关闭时固定 YT）");
		topmostItem = new ToolStripMenuItem("窗口置顶");
		lockPositionItem = new ToolStripMenuItem("锁定位置");
		silentStartItem = new ToolStripMenuItem("静默启动");
		startupItem = new ToolStripMenuItem("开机启动");
		ToolStripMenuItem toolStripMenuItem4 = new ToolStripMenuItem("退出");
		showHideItem.Click += delegate
		{
			ToggleVisibility();
		};
		toolStripMenuItem.Click += delegate
		{
			RefreshNow();
		};
		restorePositionItem.Click += delegate
		{
			RestoreToBottomRight();
		};
		toolStripMenuItem2.Click += delegate
		{
			OpenSettings();
		};
		toolStripMenuItem3.Click += delegate
		{
			OpenAchievementLog();
		};
		usageStatsItem.Click += delegate
		{
			OpenUsageStats();
		};
		lowPowerItem.Click += delegate
		{
			config.low_power_mode = !config.low_power_mode;
			ConfigStore.Save(config);
			ScheduleNextRefresh();
			UpdateTrayMenuState();
		};
		fullCountsItem.Click += delegate
		{
			config.show_full_counts = !config.show_full_counts;
			ConfigStore.Save(config);
			ReapplyCountFormat();
			UpdateTrayMenuState();
		};
		trayDataItem.Click += delegate
		{
			config.show_tray_counts = !config.show_tray_counts;
			ConfigStore.Save(config);
			UpdateTrayMenuState();
			UpdateTrayIconVisual();
		};
		topmostItem.Click += delegate
		{
			config.always_on_top = !config.always_on_top;
			ConfigStore.Save(config);
			ApplyNormalWindowMode();
			UpdateTrayMenuState();
		};
		lockPositionItem.Click += delegate
		{
			config.lock_position = !config.lock_position;
			ConfigStore.Save(config);
			UpdateTrayMenuState();
		};
		silentStartItem.Click += delegate
		{
			config.silent_start = !config.silent_start;
			ConfigStore.Save(config);
			UpdateTrayMenuState();
		};
		startupItem.Click += delegate
		{
			StartupManager.SetEnabled(!StartupManager.IsEnabled());
			UpdateTrayMenuState();
		};
		toolStripMenuItem4.Click += delegate
		{
			QuitFromApp();
		};
		trayMenu.Items.Add(showHideItem);
		trayMenu.Items.Add(toolStripMenuItem);
		trayMenu.Items.Add(restorePositionItem);
		trayMenu.Items.Add(toolStripMenuItem2);
		trayMenu.Items.Add(toolStripMenuItem3);
		trayMenu.Items.Add(usageStatsItem);
		trayMenu.Items.Add(new ToolStripSeparator());
		trayMenu.Items.Add(lowPowerItem);
		trayMenu.Items.Add(fullCountsItem);
		trayMenu.Items.Add(trayDataItem);
		trayMenu.Items.Add(topmostItem);
		trayMenu.Items.Add(lockPositionItem);
		trayMenu.Items.Add(silentStartItem);
		trayMenu.Items.Add(startupItem);
		trayMenu.Items.Add(new ToolStripSeparator());
		trayMenu.Items.Add(toolStripMenuItem4);
		trayMenu.Opening += delegate
		{
			UpdateTrayMenuState();
		};
	}

	private void BuildTrayIcon()
	{
		trayIcon = new NotifyIcon();
		trayIcon.Icon = SystemIcons.Application;
		trayIcon.Text = AppInfo.DisplayName;
		trayIcon.Visible = true;
		trayIcon.ContextMenuStrip = trayMenu;
		trayIcon.DoubleClick += delegate
		{
			ToggleVisibility();
		};
		trayIconTimer = new System.Windows.Forms.Timer();
		trayIconTimer.Interval = 5000;
		trayIconTimer.Tick += delegate
		{
			RotateTrayIconMetric();
		};
	}

	private void UpdateTrayMenuState()
	{
		showHideItem.Text = (base.Visible ? "隐藏小组件" : "显示小组件");
		lowPowerItem.Checked = config.low_power_mode;
		fullCountsItem.Checked = config.show_full_counts;
		trayDataItem.Checked = config.show_tray_counts;
		topmostItem.Checked = config.always_on_top;
		lockPositionItem.Checked = config.lock_position;
		silentStartItem.Checked = config.silent_start;
		startupItem.Checked = StartupManager.IsEnabled();
	}

	private void RestoreToBottomRight()
	{
		if (!base.Visible)
		{
			Show();
		}
		config.dock_to_tray = true;
		MoveNearTray();
		ApplyNormalWindowMode();
		UpdateTrayMenuState();
	}

	private void ToggleVisibility()
	{
		if (base.Visible)
		{
			Hide();
			return;
		}
		Show();
		MoveToSavedPosition();
		ApplyNormalWindowMode();
	}

	private void OpenAchievementLog()
	{
		using AchievementsForm achievementsForm = new AchievementsForm();
		achievementsForm.ShowDialog(this);
	}

	private void OpenSettings()
	{
		using SettingsForm settingsForm = new SettingsForm(ConfigStore.Clone(config));
		if (settingsForm.ShowDialog(this) == DialogResult.OK)
		{
			string text = OwnerYouTubeConfiguredKey(config);
			WidgetConfig resultConfig = settingsForm.ResultConfig;
			string text2 = OwnerYouTubeConfiguredKey(resultConfig);
			refreshGeneration++;
			refreshing = false;
			if (!string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
			{
				resultConfig.creator_state = null;
				latestVideoAt = null;
				lastCreatorFetch = null;
			}
			config = resultConfig;
			ConfigStore.Save(config);
			RestoreCreatorState();
			BuildUi();
			MoveToSavedPosition();
			ApplyNormalWindowMode();
			ScheduleNextRefresh();
			RefreshNow();
		}
	}

	private void AttachWidgetMouseEvents(Control control)
	{
		control.MouseDown += WidgetMouseDown;
		control.MouseMove += WidgetMouseMove;
		control.MouseUp += WidgetMouseUp;
		control.MouseClick += WidgetMouseClick;
	}

	private void AttachRowOpenEvents(Control control, int channelIndex)
	{
		control.DoubleClick += delegate
		{
			OpenChannelPage(channelIndex);
		};
	}

	private void OpenChannelPage(int channelIndex)
	{
		if (channelIndex < 0 || channelIndex >= config.channels.Count)
		{
			return;
		}
		ChannelConfig channelConfig = config.channels[channelIndex];
		string text = null;
		if (IsBilibili(channelConfig.platform))
		{
			string text2 = FirstString(channelConfig.bilibili_uid, channelConfig.uid, channelConfig.vmid);
			if (text2.Length > 0)
			{
				text = "https://space.bilibili.com/" + text2;
			}
		}
		else
		{
			FetchResult fetchResult = ResultAt(lastResults, channelIndex);
			if (fetchResult != null && !string.IsNullOrEmpty(fetchResult.YoutubeChannelId))
			{
				text = "https://www.youtube.com/channel/" + fetchResult.YoutubeChannelId;
			}
			else
			{
				string text3 = ChannelCacheKey(channelConfig);
				if (text3.StartsWith("@"))
				{
					text = "https://www.youtube.com/" + text3;
				}
				else if (text3.StartsWith("UC"))
				{
					text = "https://www.youtube.com/channel/" + text3;
				}
			}
		}
		if (text == null)
		{
			return;
		}
		try
		{
			Process.Start(text);
		}
		catch (Exception ex)
		{
			AppLogger.Error("channel-open", ex);
		}
	}

	private void WidgetMouseClick(object sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Right)
		{
			trayMenu.Show(Cursor.Position);
		}
	}

	private void WidgetMouseDown(object sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && !config.lock_position)
		{
			dragging = true;
			dragStartScreen = Cursor.Position;
			dragStartLocation = base.Location;
		}
	}

	private void WidgetMouseMove(object sender, MouseEventArgs e)
	{
		if (dragging && !config.lock_position)
		{
			Point position = Cursor.Position;
			base.Location = new Point(dragStartLocation.X + position.X - dragStartScreen.X, dragStartLocation.Y + position.Y - dragStartScreen.Y);
		}
	}

	private void WidgetMouseUp(object sender, MouseEventArgs e)
	{
		if (dragging)
		{
			dragging = false;
			config.dock_to_tray = false;
			config.position = new PositionConfig
			{
				x = base.Location.X,
				y = base.Location.Y
			};
			ConfigStore.Save(config);
			ApplyNormalWindowMode();
		}
	}

	private void MoveToSavedPosition()
	{
		if (config.dock_to_tray)
		{
			MoveNearTray();
			return;
		}
		if (config.position == null)
		{
			config.position = new PositionConfig
			{
				x = 80,
				y = 80
			};
		}
		Rectangle workingArea = Screen.FromPoint(new Point(config.position.x, config.position.y)).WorkingArea;
		int num = Math.Max(workingArea.Left, Math.Min(config.position.x, workingArea.Right - base.Width));
		int num2 = Math.Max(workingArea.Top, Math.Min(config.position.y, workingArea.Bottom - base.Height));
		base.Location = new Point(num, num2);
	}

	private void MoveNearTray()
	{
		Rectangle workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
		base.Location = new Point(workingArea.Right - base.Width - 12, workingArea.Bottom - base.Height - 12);
		config.position = new PositionConfig
		{
			x = base.Location.X,
			y = base.Location.Y
		};
		ConfigStore.Save(config);
	}

	private void ApplyNormalWindowMode()
	{
		if (base.IsHandleCreated)
		{
			NativeMethods.SetParent(base.Handle, IntPtr.Zero);
			NativeMethods.SetWindowAsPopup(base.Handle);
			base.TopMost = config.always_on_top;
			NativeMethods.SetWindowPos(base.Handle, IntPtr.Zero, base.Location.X, base.Location.Y, base.Width, base.Height, 116u);
		}
	}

	private int EffectiveRefreshMinutes()
	{
		int num = ((config.refresh_minutes <= 0) ? 60 : config.refresh_minutes);
		if (config.low_power_mode && num < 60)
		{
			num = 60;
		}
		if (num < 1)
		{
			num = 1;
		}
		return num;
	}

	private void ScheduleNextRefresh()
	{
		ScheduleRefreshAfterMinutes(EffectiveRefreshMinutes());
	}

	private void ScheduleRefreshAfterMinutes(int minutes)
	{
		if (refreshTimer != null)
		{
			refreshTimer.Stop();
			refreshTimer.Interval = Math.Max(1000, minutes * 60 * 1000);
			refreshTimer.Start();
		}
	}

	private void RefreshNow()
	{
		if (refreshing)
		{
			return;
		}
		UpdateSlogan();
		if (refreshTimer != null)
		{
			refreshTimer.Stop();
		}
		refreshing = true;
		if (statusLabel != null)
		{
			statusLabel.Text = "正在刷新…";
			statusLabel.ForeColor = Theme.TextMuted;
		}
		SetTrayText("正在刷新");
		foreach (ChannelRow row in rows)
		{
			row.DetailLabel.Text = "正在读取";
			row.DetailLabel.ForeColor = Theme.TextMuted;
		}
		WidgetConfig snapshot = ConfigStore.Clone(config);
		int generation = ++refreshGeneration;
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				RefreshPayload payload = RefreshService.Fetch(snapshot, () => generation != refreshGeneration || appQuitting);
				if (generation != refreshGeneration || appQuitting || base.IsDisposed || !base.IsHandleCreated)
				{
					return;
				}
				BeginInvoke((Action)delegate
				{
					if (generation == refreshGeneration && !appQuitting)
					{
						ApplyResults(payload.Results, payload.Creator);
					}
				});
			}
			catch (Exception ex)
			{
				AppLogger.Error("refresh", ex);
				if (generation == refreshGeneration && !appQuitting && !base.IsDisposed && base.IsHandleCreated)
				{
					BeginInvoke((Action)delegate
					{
						if (generation == refreshGeneration)
						{
							refreshing = false;
							statusLabel.Text = "刷新异常，稍后重试";
							statusLabel.ForeColor = Theme.Error;
							ScheduleNextRefresh();
						}
					});
				}
			}
		});
	}

	private void UpdateSlogan()
	{
		if (sloganLabel != null)
		{
			string text = SloganProvider.Next();
			sloganLabel.Text = SloganProvider.ToDisplayText(text);
			rowToolTip.SetToolTip(sloganLabel, text + "\n双击编辑标语文件");
		}
	}

	private void OpenSloganFile()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = SloganProvider.SloganPath,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			AppLogger.Error("slogan-open", ex);
			MessageBox.Show("无法打开励志标语文件。", AppInfo.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void RenderCachedCountsForStartup()
	{
		List<FetchResult> list = BuildCachedResults();
		bool flag = false;
		foreach (ChannelRow row in rows)
		{
			FetchResult fetchResult = ResultAt(list, row.ChannelIndex);
			if (fetchResult != null)
			{
				if (fetchResult.HasCached)
				{
					flag = true;
					row.CountLabel.SetCount(FormatDisplayCount(fetchResult.CachedCount));
					row.CountLabel.ForeColor = Theme.TextPrimary;
					row.DetailLabel.Text = "上次数据 " + FormatCachedTime(fetchResult.CachedAt);
					row.DetailLabel.ForeColor = Theme.TextMuted;
				}
				else
				{
					row.CountLabel.SetCount("--");
					row.CountLabel.ForeColor = Theme.TextMuted;
					row.DetailLabel.Text = "等待刷新";
					row.DetailLabel.ForeColor = Theme.TextMuted;
				}
			}
		}
		lastResults = list;
		UpdateMilestoneProgress(list);
		ApplyBenchmarkDeltas(list);
		UpdateSparklines(list);
		UpdateTrayMetrics(list);
		UpdateTrayText(list);
		UpdateTrayIconVisual();
		UpdateCreatorCard();
		List<string> list2 = CollectCachedMilestoneAchievements(list);
		if (list2.Count > 0)
		{
			ConfigStore.Save(config);
			trayIcon?.ShowBalloonTip(6000, AppInfo.DisplayName + " · 成就", string.Join("\n", list2.ToArray()), ToolTipIcon.Info);
			ShowAchievementCelebration(list2.Count);
		}
		if (statusLabel != null)
		{
			statusLabel.Text = (flag ? "已载入上次数据 · 5 分钟后刷新" : "首次刷新将在 5 分钟后开始");
			statusLabel.ForeColor = Theme.TextMuted;
		}
	}

	private void ApplyResults(List<FetchResult> results, CreatorFetchData creator)
	{
		lastResults = results;
		lastCreatorFetch = creator;
		weeklyMetricMode++;
		int num = 0;
		int num2 = 0;
		foreach (ChannelRow row in rows)
		{
			FetchResult fetchResult = ResultAt(results, row.ChannelIndex);
			if (fetchResult != null)
			{
				if (fetchResult.Ok)
				{
					row.CountLabel.SetCount(FormatDisplayCount(fetchResult.Count));
					row.CountLabel.ForeColor = Theme.TextPrimary;
					row.DetailLabel.Text = ComposeStatusDetail("已更新", row.ChannelIndex, fetchResult.Count);
					row.DetailLabel.ForeColor = Theme.Success;
				}
				else if (fetchResult.HasCached)
				{
					row.CountLabel.SetCount(FormatDisplayCount(fetchResult.CachedCount));
					row.CountLabel.ForeColor = Theme.TextPrimary;
					row.DetailLabel.Text = "刷新失败";
					row.DetailLabel.ForeColor = Theme.Warning;
				}
				else
				{
					row.CountLabel.SetCount("--");
					row.CountLabel.ForeColor = Theme.Error;
					row.DetailLabel.Text = Truncate(fetchResult.Error, 20);
					row.DetailLabel.ForeColor = Theme.Error;
				}
			}
		}
		for (int i = 0; i < results.Count; i++)
		{
			if (results[i].Ok)
			{
				num++;
			}
			else if (results[i].HasCached)
			{
				num2++;
			}
		}
		int achievementCount;
		List<string> list = CollectEvents(results, out achievementCount);
		if (num > 0 || list.Count > 0)
		{
			string text = SaveSuccessfulCounts(results, creator);
			if (text != null)
			{
				list.Insert(0, text);
			}
		}
		string text2 = DateTime.Now.ToString("HH:mm");
		if (results.Count == 0)
		{
			statusLabel.Text = "尚未配置频道";
			statusLabel.ForeColor = Theme.TextMuted;
		}
		else if (num == results.Count)
		{
			statusLabel.Text = "已更新 " + text2;
			statusLabel.ForeColor = Theme.TextMuted;
		}
		else if (num > 0)
		{
			statusLabel.Text = "部分更新 " + text2;
			statusLabel.ForeColor = Theme.TextMuted;
		}
		else if (num2 > 0)
		{
			statusLabel.Text = "刷新失败 · 显示旧数据 " + text2;
			statusLabel.ForeColor = Theme.Warning;
		}
		else
		{
			statusLabel.Text = "全部读取失败 " + text2;
			statusLabel.ForeColor = Theme.Error;
		}
		if (statusDot != null)
		{
			if (results.Count == 0)
			{
				statusDot.ForeColor = Theme.TextMuted;
			}
			else if (num == results.Count)
			{
				statusDot.ForeColor = Theme.Success;
			}
			else if (num > 0 || num2 > 0)
			{
				statusDot.ForeColor = Theme.Warning;
			}
			else
			{
				statusDot.ForeColor = Theme.Error;
			}
		}
		refreshing = false;
		if (creator != null && creator.LatestVideoAt.HasValue)
		{
			latestVideoAt = creator.LatestVideoAt;
		}
		UpdateMilestoneProgress(results);
		ApplyBenchmarkDeltas(results);
		UpdateSparklines(results);
		UpdateTrayMetrics(results);
		UpdateTrayText(results);
		UpdateTrayIconVisual();
		UpdateCreatorCard();
		if (list.Count > 0 && trayIcon != null)
		{
			trayIcon.ShowBalloonTip(6000, AppInfo.DisplayName, string.Join("\n", list.ToArray()), ToolTipIcon.Info);
		}
		ShowAchievementCelebration(achievementCount);
		ScheduleNextRefresh();
	}

	private List<string> CollectEvents(List<FetchResult> results, out int achievementCount)
	{
		List<string> list = new List<string>();
		achievementCount = 0;
		for (int i = 0; i < results.Count && i < config.channels.Count; i++)
		{
			if (!results[i].Ok)
			{
				continue;
			}
			ChannelConfig channelConfig = config.channels[i];
			CachedCountConfig cachedCountConfig = FindCachedCount(config, channelConfig);
			HashSet<long> hashSet = (channelConfig.benchmark ? null : GetAchievedMilestones(channelConfig));
			List<string> collection = MilestoneTracker.CheckChannel(SafeText(channelConfig.label, channelConfig.platform), channelConfig.benchmark, cachedCountConfig != null, cachedCountConfig?.count ?? 0, results[i].Count, config.milestones, hashSet, config.surge_alert_percent);
			list.AddRange(collection);
			if (!channelConfig.benchmark && config.milestones != null)
			{
				foreach (long milestone in config.milestones)
				{
					if (milestone > 0 && results[i].Count >= milestone && !hashSet.Contains(milestone))
					{
						achievementCount++;
					}
				}
			}
			if (!channelConfig.benchmark)
			{
				MarkReachedMilestones(channelConfig, results[i].Count);
			}
		}
		foreach (KeyValuePair<int, int> item in targetOfBenchmark)
		{
			int key = item.Key;
			int value = item.Value;
			FetchResult fetchResult = ResultAt(results, key);
			FetchResult result = ResultAt(results, value);
			if (!HasEffectiveCount(fetchResult) || !HasEffectiveCount(result))
			{
				continue;
			}
			long num = EffectiveCount(result);
			long num2 = EffectiveCount(fetchResult);
			if (num <= num2 || num2 <= 0)
			{
				continue;
			}
			ChannelConfig channelConfig2 = config.channels[key];
			string text = SafeText(channelConfig2.label, channelConfig2.platform);
			long num3 = num - num2;
			if (!OvertakeAchievementRecorded(channelConfig2))
			{
				string text5 = "恭喜，您又超了一个！";
				list.Add(text5);
				MilestoneTracker.LogAchievement(text5 + "（已超越 " + text + "，领先 " + FormatDisplayCount(num3) + "）");
				MarkOvertakeAchievement(channelConfig2);
				achievementCount++;
			}
			string text2 = null;
			if (num3 * 100 <= num * config.overtake_warn_percent)
			{
				text2 = "「" + text + "」即将超越您！当前仅领先 " + FormatDisplayCount(num3);
			}
			else if (fetchResult.Ok)
			{
				CachedCountConfig cachedCountConfig2 = FindCachedCount(config, channelConfig2);
				if (cachedCountConfig2 != null && cachedCountConfig2.count > 0)
				{
					double num4 = (double)(fetchResult.Count - cachedCountConfig2.count) / (double)cachedCountConfig2.count;
					if (num4 * 100.0 >= (double)config.surge_alert_percent && num2 * 2 >= num)
					{
						text2 = "「" + text + "」涨势迅猛（+" + (num4 * 100.0).ToString("0.#") + "%），即将超越您！";
					}
				}
			}
			if (text2 != null && !WarnedToday(channelConfig2))
			{
				list.Add(text2);
				MilestoneTracker.LogWarn(text2);
				MarkWarnedToday(channelConfig2);
			}
		}
		return list;
	}

	private void ShowAchievementCelebration(int achievementCount)
	{
		if (achievementCount > 0)
		{
			if (!base.IsHandleCreated || !base.Visible)
			{
				pendingAchievementCelebrations += achievementCount;
			}
			else
			{
				FireworksForm.ShowCelebration(this, achievementCount);
			}
		}
	}

	private List<string> CollectCachedMilestoneAchievements(List<FetchResult> results)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < results.Count && i < config.channels.Count; i++)
		{
			ChannelConfig channelConfig = config.channels[i];
			if (!channelConfig.benchmark && results[i].HasCached)
			{
				long cachedCount = results[i].CachedCount;
				HashSet<long> achievedMilestones = GetAchievedMilestones(channelConfig);
				list.AddRange(MilestoneTracker.CheckChannel(SafeText(channelConfig.label, channelConfig.platform), isBenchmark: false, hasOldCount: true, cachedCount, cachedCount, config.milestones, achievedMilestones, config.surge_alert_percent));
				MarkReachedMilestones(channelConfig, cachedCount);
			}
		}
		return list;
	}

	private HashSet<long> GetAchievedMilestones(ChannelConfig channel)
	{
		HashSet<long> hashSet = new HashSet<long>();
		string b = NormalizePlatform(channel.platform);
		string b2 = ChannelCacheKey(channel);
		foreach (AchievedMilestoneConfig achieved_milestone in config.achieved_milestones)
		{
			if (achieved_milestone != null && string.Equals(achieved_milestone.platform, b, StringComparison.OrdinalIgnoreCase) && string.Equals(achieved_milestone.key, b2, StringComparison.OrdinalIgnoreCase))
			{
				hashSet.Add(achieved_milestone.milestone);
			}
		}
		string channelLabel = SafeText(channel.label, channel.platform);
		foreach (long milestone in config.milestones)
		{
			if (MilestoneTracker.WasMilestoneLogged(channelLabel, milestone))
			{
				hashSet.Add(milestone);
			}
		}
		return hashSet;
	}

	private void MarkReachedMilestones(ChannelConfig channel, long currentCount)
	{
		string text = NormalizePlatform(channel.platform);
		string text2 = ChannelCacheKey(channel);
		HashSet<long> hashSet = new HashSet<long>();
		foreach (AchievedMilestoneConfig achieved_milestone in config.achieved_milestones)
		{
			if (achieved_milestone != null && string.Equals(achieved_milestone.platform, text, StringComparison.OrdinalIgnoreCase) && string.Equals(achieved_milestone.key, text2, StringComparison.OrdinalIgnoreCase))
			{
				hashSet.Add(achieved_milestone.milestone);
			}
		}
		foreach (long milestone in config.milestones)
		{
			if (milestone > 0 && currentCount >= milestone && !hashSet.Contains(milestone))
			{
				config.achieved_milestones.Add(new AchievedMilestoneConfig
				{
					platform = text,
					key = text2,
					milestone = milestone
				});
				hashSet.Add(milestone);
			}
		}
	}

	private bool WarnedToday(ChannelConfig channel)
	{
		string b = ChannelCacheKey(channel);
		string b2 = NormalizePlatform(channel.platform);
		string text = DateTime.Now.ToString("yyyy-MM-dd");
		foreach (WarnRecordConfig warn_record in config.warn_records)
		{
			if (warn_record != null && string.Equals(warn_record.key, b, StringComparison.OrdinalIgnoreCase) && string.Equals(warn_record.platform, b2, StringComparison.OrdinalIgnoreCase))
			{
				return warn_record.date == text;
			}
		}
		return false;
	}

	private bool OvertakeAchievementRecorded(ChannelConfig channel)
	{
		string key = ChannelCacheKey(channel);
		string platform = NormalizePlatform(channel.platform);
		foreach (AchievementRecordConfig record in config.achievement_records)
		{
			if (record != null && string.Equals(record.id, "benchmark_overtake", StringComparison.OrdinalIgnoreCase) && string.Equals(record.key, key, StringComparison.OrdinalIgnoreCase) && string.Equals(record.platform, platform, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		foreach (WarnRecordConfig oldRecord in config.warn_records)
		{
			if (oldRecord != null && string.Equals(oldRecord.key, key + ":overtaken", StringComparison.OrdinalIgnoreCase) && string.Equals(oldRecord.platform, platform, StringComparison.OrdinalIgnoreCase))
			{
				MarkOvertakeAchievement(channel);
				return true;
			}
		}
		return false;
	}

	private void MarkOvertakeAchievement(ChannelConfig channel)
	{
		string text = ChannelCacheKey(channel);
		string text2 = NormalizePlatform(channel.platform);
		foreach (AchievementRecordConfig record in config.achievement_records)
		{
			if (record != null && string.Equals(record.id, "benchmark_overtake", StringComparison.OrdinalIgnoreCase) && string.Equals(record.key, text, StringComparison.OrdinalIgnoreCase) && string.Equals(record.platform, text2, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
		}
		config.achievement_records.Add(new AchievementRecordConfig
		{
			id = "benchmark_overtake",
			platform = text2,
			key = text,
			achieved_at = DateTime.Now.ToString("o")
		});
	}

	private void MarkWarnedToday(ChannelConfig channel)
	{
		string text = ChannelCacheKey(channel);
		string text2 = NormalizePlatform(channel.platform);
		string date = DateTime.Now.ToString("yyyy-MM-dd");
		foreach (WarnRecordConfig warn_record in config.warn_records)
		{
			if (warn_record != null && string.Equals(warn_record.key, text, StringComparison.OrdinalIgnoreCase) && string.Equals(warn_record.platform, text2, StringComparison.OrdinalIgnoreCase))
			{
				warn_record.date = date;
				return;
			}
		}
		config.warn_records.Add(new WarnRecordConfig
		{
			platform = text2,
			key = text,
			date = date
		});
	}

	private void ReapplyCountFormat()
	{
		if (lastResults == null || lastResults.Count == 0)
		{
			UpdateTrayIconVisual();
			return;
		}
		foreach (ChannelRow row in rows)
		{
			FetchResult fetchResult = ResultAt(lastResults, row.ChannelIndex);
			if (fetchResult != null)
			{
				if (fetchResult.Ok)
				{
					row.CountLabel.SetCount(FormatDisplayCount(fetchResult.Count));
				}
				else if (fetchResult.HasCached)
				{
					row.CountLabel.SetCount(FormatDisplayCount(fetchResult.CachedCount));
				}
			}
		}
		UpdateMilestoneProgress(lastResults);
		ApplyBenchmarkDeltas(lastResults);
		UpdateTrayMetrics(lastResults);
		UpdateTrayText(lastResults);
		UpdateTrayIconVisual();
	}

	private List<FetchResult> BuildCachedResults()
	{
		List<FetchResult> list = new List<FetchResult>();
		for (int i = 0; i < config.channels.Count; i++)
		{
			CachedCountConfig cachedCountConfig = FindCachedCount(config, config.channels[i]);
			list.Add(new FetchResult
			{
				Ok = false,
				HasCached = (cachedCountConfig != null),
				CachedCount = (cachedCountConfig?.count ?? 0),
				CachedAt = cachedCountConfig?.updated_at,
				Error = ((cachedCountConfig == null) ? "等待刷新" : null)
			});
		}
		return list;
	}

	private string SaveSuccessfulCounts(List<FetchResult> results, CreatorFetchData creator)
	{
		DateTime? dateTime = creator?.LatestVideoAt;
		string last_video_title = creator?.LatestVideoTitle;
		string text = creator?.ChannelKey;
		for (int i = 0; i < results.Count && i < config.channels.Count; i++)
		{
			if (results[i].Ok && !IsBilibili(config.channels[i].platform) && !string.IsNullOrEmpty(results[i].YoutubeChannelId))
			{
				ChannelIdentity.ApplyResolvedYouTubeId(config, config.channels[i], results[i].YoutubeChannelId);
			}
		}
		RollDailyBaselines(results);
		string result = CanGenerateWeeklyReport(results) ? BuildWeeklyReportAndRollBaselines(results, creator?.VideoTimes) : null;
		EnsureCurrentWeekMetricBaselines(results);
		string text2 = DateTime.Now.ToString("o");
		for (int j = 0; j < results.Count && j < config.channels.Count; j++)
		{
			if (results[j].Ok)
			{
				ChannelConfig channelConfig = config.channels[j];
				CachedCountConfig cachedCountConfig = FindCachedCount(config, channelConfig);
				if (cachedCountConfig == null)
				{
					cachedCountConfig = new CachedCountConfig();
					config.cached_counts.Add(cachedCountConfig);
				}
				cachedCountConfig.platform = NormalizePlatform(channelConfig.platform);
				cachedCountConfig.key = ChannelCacheKey(channelConfig);
				cachedCountConfig.label = SafeText(channelConfig.label, channelConfig.platform);
				cachedCountConfig.count = results[j].Count;
				cachedCountConfig.updated_at = text2;
			}
		}
		if (creator != null && !string.IsNullOrEmpty(text))
		{
			if (config.creator_state == null)
			{
				config.creator_state = new CreatorStateConfig();
			}
			config.creator_state.channel_key = text;
			config.creator_state.configured_channel_key = OwnerYouTubeConfiguredKey(config);
			config.creator_state.checked_at = text2;
			if (creator.CompleteHistoryYear > 0)
			{
				config.creator_state.monthly_history_year = creator.CompleteHistoryYear;
				config.creator_state.monthly_history_complete = true;
				config.creator_state.monthly_history_retry_after = null;
			}
			else if (creator.FullHistoryAttempted)
			{
				config.creator_state.monthly_history_year = DateTime.Now.Year;
				config.creator_state.monthly_history_complete = false;
				config.creator_state.monthly_history_retry_after = DateTime.Now.AddDays(creator.HistoryTruncated ? 7.0 : 1.0).ToString("o");
			}
			int num = RecordMonthlyUpdates(text, creator.VideoTimes, text2, !creator.FullHistoryAttempted);
			if (num > 0)
			{
				string text3 = "额外投稿奖励：获得 " + num + " 张投稿补签卡。";
				MilestoneTracker.LogAchievement(text3);
				result = ((result == null) ? text3 : (text3 + "\n" + result));
			}
			if (dateTime.HasValue)
			{
				config.creator_state.last_video_at = dateTime.Value.ToString("o");
				config.creator_state.last_video_title = last_video_title;
			}
		}
		ConfigStore.Save(config);
		return result;
	}

	private bool CanGenerateWeeklyReport(List<FetchResult> results)
	{
		return WidgetRules.CanGenerateWeeklyReport(config.channels, results);
	}

	private int RecordMonthlyUpdates(string channelKey, List<DateTime> videoTimes, string detectedAt, bool rewardNewUploads)
	{
		return CreatorCheckinService.RecordMonthlyUploads(config, channelKey, videoTimes, detectedAt, rewardNewUploads);
	}

	private void EnsureCurrentWeekMetricBaselines(List<FetchResult> results)
	{
		if (results == null)
		{
			return;
		}
		string text = WeekMonday(DateTime.Now).ToString("yyyy-MM-dd");
		for (int i = 0; i < results.Count && i < config.channels.Count; i++)
		{
			FetchResult fetchResult = results[i];
			if (fetchResult == null || !fetchResult.Ok)
			{
				continue;
			}
			ChannelConfig channelConfig = config.channels[i];
			if (channelConfig.benchmark)
			{
				continue;
			}
			string text2 = ChannelCacheKey(channelConfig);
			string text3 = NormalizePlatform(channelConfig.platform);
			EnsureWeeklyBaseline(text3, text2, text, fetchResult.Count);
		}
	}

	private void EnsureWeeklyBaseline(string platform, string key, string thisMonday, long count)
	{
		DailyBaselineConfig dailyBaselineConfig = FindWeeklyBaseline(platform, key);
		if (dailyBaselineConfig == null)
		{
			config.weekly_baselines.Add(new DailyBaselineConfig
			{
				platform = platform,
				key = key,
				date = thisMonday,
				count = count
			});
		}
		else if (dailyBaselineConfig.date != thisMonday)
		{
			dailyBaselineConfig.date = thisMonday;
			dailyBaselineConfig.count = count;
		}
	}

	private void RollDailyBaselines(List<FetchResult> results)
	{
		string text = DateTime.Now.ToString("yyyy-MM-dd");
		for (int i = 0; i < results.Count && i < config.channels.Count; i++)
		{
			if (!results[i].Ok)
			{
				continue;
			}
			ChannelConfig channelConfig = config.channels[i];
			string text2 = ChannelCacheKey(channelConfig);
			string text3 = NormalizePlatform(channelConfig.platform);
			DailyBaselineConfig dailyBaselineConfig = FindDailyBaseline(text3, text2);
			if (dailyBaselineConfig == null)
			{
				dailyBaselineConfig = new DailyBaselineConfig
				{
					platform = text3,
					key = text2
				};
				config.daily_baselines.Add(dailyBaselineConfig);
				CachedCountConfig cachedCountConfig = FindCachedCount(config, channelConfig);
				dailyBaselineConfig.date = text;
				dailyBaselineConfig.count = cachedCountConfig?.count ?? results[i].Count;
			}
			else if (dailyBaselineConfig.date != text)
			{
				CachedCountConfig cachedCountConfig2 = FindCachedCount(config, channelConfig);
				dailyBaselineConfig.date = text;
				dailyBaselineConfig.count = cachedCountConfig2?.count ?? results[i].Count;
			}
			bool flag = false;
			foreach (DailyBaselineConfig item in config.daily_history)
			{
				if (item != null && item.date == text && string.Equals(item.key, text2, StringComparison.OrdinalIgnoreCase) && string.Equals(item.platform, text3, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				config.daily_history.Add(new DailyBaselineConfig
				{
					platform = text3,
					key = text2,
					date = text,
					count = results[i].Count
				});
			}
		}
		string cutoff = DateTime.Now.Date.AddDays(-30.0).ToString("yyyy-MM-dd");
		config.daily_history.RemoveAll((DailyBaselineConfig record) => record == null || string.CompareOrdinal(record.date, cutoff) < 0);
	}

	private string BuildWeeklyReportAndRollBaselines(List<FetchResult> results, List<DateTime> videoTimes)
	{
		DateTime dateTime = WeekMonday(DateTime.Now);
		string text = dateTime.ToString("yyyy-MM-dd");
		if (WeeklyReportStore.HasReportForWeek(dateTime))
		{
			return null;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		for (int i = 0; i < results.Count && i < config.channels.Count; i++)
		{
			ChannelConfig channelConfig = config.channels[i];
			if (channelConfig.benchmark || !results[i].Ok)
			{
				continue;
			}
			string key = ChannelCacheKey(channelConfig);
			string platform = NormalizePlatform(channelConfig.platform);
			list3.Add(SafeText(channelConfig.label, channelConfig.platform) + " " + FormatDisplayCount(results[i].Count));
			DailyBaselineConfig dailyBaselineConfig = FindWeeklyBaseline(platform, key);
			if (dailyBaselineConfig == null)
			{
				DailyBaselineConfig dailyBaselineConfig2 = FindReportHistoryBaseline(platform, key, dateTime);
				if (dailyBaselineConfig2 != null)
				{
					AddWeeklyComparison(list, list2, channelConfig, results[i].Count, dailyBaselineConfig2.count);
				}
				config.weekly_baselines.Add(new DailyBaselineConfig
				{
					platform = platform,
					key = key,
					date = text,
					count = results[i].Count
				});
			}
			else if (dailyBaselineConfig.date != text)
			{
				AddWeeklyComparison(list, list2, channelConfig, results[i].Count, dailyBaselineConfig.count);
				dailyBaselineConfig.date = text;
				dailyBaselineConfig.count = results[i].Count;
			}
			else
			{
				DailyBaselineConfig dailyBaselineConfig3 = FindReportHistoryBaseline(platform, key, dateTime);
				if (dailyBaselineConfig3 != null)
				{
					AddWeeklyComparison(list, list2, channelConfig, results[i].Count, dailyBaselineConfig3.count);
				}
			}
		}
		string text2 = ((list2.Count > 0) ? ("本周对比上周增长率：" + string.Join("，", list2.ToArray()) + "。") : "本周对比上周增长率：暂无可比基线。") + Environment.NewLine + ((list.Count > 0) ? ("周报：上周 " + string.Join("，", list.ToArray())) : ("周报：上周暂无可比基线；当前 " + ((list3.Count > 0) ? string.Join("，", list3.ToArray()) : "暂无频道数据") + "。已建立本周基线"));
		if (videoTimes != null)
		{
			DateTime dateTime2 = dateTime.AddDays(-7.0);
			int num = 0;
			foreach (DateTime videoTime in videoTimes)
			{
				DateTime dateTime3 = videoTime.ToLocalTime();
				if (dateTime3 >= dateTime2 && dateTime3 < dateTime)
				{
					num++;
				}
			}
			text2 = text2 + "，发布 " + num + " 支视频";
		}
		WeeklyReportStore.Append(dateTime, text2);
		MilestoneTracker.LogReport(text2);
		return text2;
	}

	private void AddWeeklyComparison(List<string> parts, List<string> growthParts, ChannelConfig channel, long currentCount, long baselineCount)
	{
		long num = currentCount - baselineCount;
		string text = SafeText(channel.label, channel.platform);
		parts.Add(text + " " + ((num >= 0) ? "+" : "-") + FormatDisplayCount(Math.Abs(num)));
		if (baselineCount > 0)
		{
			double num2 = (double)num * 100.0 / (double)baselineCount;
			growthParts.Add(text + " " + ((num2 >= 0.0) ? "+" : "") + num2.ToString("0.##") + "%");
		}
	}

	private DailyBaselineConfig FindReportHistoryBaseline(string platform, string key, DateTime thisMonday)
	{
		string strB = thisMonday.AddDays(-7.0).ToString("yyyy-MM-dd");
		string strB2 = thisMonday.ToString("yyyy-MM-dd");
		DailyBaselineConfig dailyBaselineConfig = null;
		foreach (DailyBaselineConfig item in config.daily_history)
		{
			if (item != null && string.Equals(item.platform, platform, StringComparison.OrdinalIgnoreCase) && string.Equals(item.key, key, StringComparison.OrdinalIgnoreCase) && string.CompareOrdinal(item.date, strB) >= 0 && string.CompareOrdinal(item.date, strB2) < 0 && (dailyBaselineConfig == null || string.CompareOrdinal(item.date, dailyBaselineConfig.date) < 0))
			{
				dailyBaselineConfig = item;
			}
		}
		return dailyBaselineConfig;
	}

	private DailyBaselineConfig FindWeeklyBaseline(string platform, string key)
	{
		foreach (DailyBaselineConfig weekly_baseline in config.weekly_baselines)
		{
			if (weekly_baseline != null && string.Equals(weekly_baseline.key, key, StringComparison.OrdinalIgnoreCase) && string.Equals(weekly_baseline.platform, platform, StringComparison.OrdinalIgnoreCase))
			{
				return weekly_baseline;
			}
		}
		return null;
	}

	private static DateTime WeekMonday(DateTime date)
	{
		int num = (int)(date.DayOfWeek + 6) % 7;
		return date.Date.AddDays(-num);
	}

	private double? DailyAverageGain(int channelIndex, long currentCount)
	{
		if (channelIndex < 0 || channelIndex >= config.channels.Count)
		{
			return null;
		}
		ChannelConfig channelConfig = config.channels[channelIndex];
		string b = ChannelCacheKey(channelConfig);
		string b2 = NormalizePlatform(channelConfig.platform);
		DateTime date = DateTime.Now.Date;
		string strB = date.AddDays(-7.0).ToString("yyyy-MM-dd");
		string strB2 = date.ToString("yyyy-MM-dd");
		DailyBaselineConfig dailyBaselineConfig = null;
		foreach (DailyBaselineConfig item in config.daily_history)
		{
			if (item != null && string.Equals(item.key, b, StringComparison.OrdinalIgnoreCase) && string.Equals(item.platform, b2, StringComparison.OrdinalIgnoreCase) && string.CompareOrdinal(item.date, strB) >= 0 && string.CompareOrdinal(item.date, strB2) < 0 && (dailyBaselineConfig == null || string.CompareOrdinal(item.date, dailyBaselineConfig.date) < 0))
			{
				dailyBaselineConfig = item;
			}
		}
		if (dailyBaselineConfig == null || !DateTime.TryParse(dailyBaselineConfig.date, out var result))
		{
			return null;
		}
		int days = (date - result.Date).Days;
		if (days < 1)
		{
			return null;
		}
		return (double)(currentCount - dailyBaselineConfig.count) / (double)days;
	}

	private DailyBaselineConfig FindDailyBaseline(string platform, string key)
	{
		foreach (DailyBaselineConfig daily_baseline in config.daily_baselines)
		{
			if (daily_baseline != null && string.Equals(daily_baseline.key, key, StringComparison.OrdinalIgnoreCase) && string.Equals(daily_baseline.platform, platform, StringComparison.OrdinalIgnoreCase))
			{
				return daily_baseline;
			}
		}
		return null;
	}

	private long? GetTodayDelta(int channelIndex, long currentCount)
	{
		if (channelIndex < 0 || channelIndex >= config.channels.Count)
		{
			return null;
		}
		ChannelConfig channelConfig = config.channels[channelIndex];
		DailyBaselineConfig dailyBaselineConfig = FindDailyBaseline(NormalizePlatform(channelConfig.platform), ChannelCacheKey(channelConfig));
		if (dailyBaselineConfig == null || dailyBaselineConfig.date != DateTime.Now.ToString("yyyy-MM-dd"))
		{
			return null;
		}
		return currentCount - dailyBaselineConfig.count;
	}

	private string ComposeStatusDetail(string statusText, int channelIndex, long currentCount)
	{
		List<string> list = new List<string>();
		long? todayDelta = GetTodayDelta(channelIndex, currentCount);
		if (todayDelta.HasValue && todayDelta.Value != 0L)
		{
			list.Add("今日" + ((todayDelta.Value > 0) ? "+" : "-") + FormatDisplayCount(Math.Abs(todayDelta.Value)));
		}
		if (!benchmarksOfTarget.ContainsKey(channelIndex))
		{
			long num = NextMilestone(currentCount);
			if (num > 0)
			{
				list.Add("距 " + FormatCompactCount(num) + " 差 " + FormatDisplayCount(num - currentCount));
				double? num2 = DailyAverageGain(channelIndex, currentCount);
				if (num2.HasValue && num2.Value > 0.0)
				{
					int num3 = (int)Math.Ceiling((double)(num - currentCount) / num2.Value);
					if (num3 <= 365)
					{
						list.Add("约" + num3 + "天");
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return statusText;
		}
		return string.Join(" · ", list.ToArray());
	}

	private string FormatSignedCount(long? value)
	{
		if (!value.HasValue)
		{
			return "—";
		}
		return ((value.Value >= 0) ? "+" : "-") + FormatDisplayCount(Math.Abs(value.Value));
	}

	private long NextMilestone(long currentCount)
	{
		return WidgetRules.NextMilestone(config.milestones, currentCount);
	}

	private long PreviousMilestone(long currentCount)
	{
		return WidgetRules.PreviousMilestone(config.milestones, currentCount);
	}

	private void UpdateMilestoneProgress(List<FetchResult> results)
	{
		foreach (ChannelRow row in rows)
		{
			if (row.MilestoneLabel == null || row.MilestoneProgressBar == null)
			{
				continue;
			}
			FetchResult result = ResultAt(results, row.ChannelIndex);
			if (!HasEffectiveCount(result))
			{
				row.MilestoneLabel.Text = "-- 进度 --%";
				row.MilestoneProgressBar.SetRatio(null);
				rowToolTip.SetToolTip(row.MilestoneLabel, "等待粉丝数据");
				rowToolTip.SetToolTip(row.MilestoneProgressBar, "等待粉丝数据");
				continue;
			}
			long num = EffectiveCount(result);
			long num2 = NextMilestone(num);
			if (num2 <= 0)
			{
				row.MilestoneLabel.Text = "全部进度 100%";
				row.MilestoneProgressBar.SetRatio(1.0);
				string caption = "已完成全部配置的粉丝里程碑（当前 " + FormatFullCount(num) + "）";
				rowToolTip.SetToolTip(row.MilestoneLabel, caption);
				rowToolTip.SetToolTip(row.MilestoneProgressBar, caption);
				continue;
			}
			long num3 = PreviousMilestone(num);
			long num4 = num2 - num3;
			double val = ((num4 > 0) ? ((double)(num - num3) / (double)num4) : 0.0);
			val = Math.Max(0.0, Math.Min(1.0, val));
			row.MilestoneLabel.Text = FormatMilestoneTarget(num2) + " 进度 " + (val * 100.0).ToString("0.#") + "%";
			row.MilestoneProgressBar.SetRatio(val);
			string caption2 = "下一个成就：" + FormatFullCount(num2) + "（当前 " + FormatFullCount(num) + "，本阶段 " + (val * 100.0).ToString("0.#") + "%）";
			rowToolTip.SetToolTip(row.MilestoneLabel, caption2);
			rowToolTip.SetToolTip(row.MilestoneProgressBar, caption2);
		}
	}

	private static FetchResult ResultAt(List<FetchResult> results, int index)
	{
		if (results == null || index < 0 || index >= results.Count)
		{
			return null;
		}
		return results[index];
	}

	private void UpdateTrayMetrics(List<FetchResult> results)
	{
		trayMetrics.Clear();
		staticYouTubeTrayMetric = null;
		for (int i = 0; i < results.Count && i < config.channels.Count; i++)
		{
			ChannelConfig channelConfig = config.channels[i];
			if (channelConfig.benchmark)
			{
				continue;
			}
			bool flag = IsBilibili(channelConfig.platform);
			if (results[i].Ok || results[i].HasCached)
			{
				TrayMetric item = new TrayMetric
				{
					Tag = (flag ? "B" : "YT"),
					Count = (results[i].Ok ? FormatTrayCount(results[i].Count) : FormatTrayCount(results[i].CachedCount)),
					Accent = (flag ? Theme.BiliAccent : Theme.YouTubeAccent)
				};
				trayMetrics.Add(item);
				if (!flag && staticYouTubeTrayMetric == null)
				{
					staticYouTubeTrayMetric = item;
				}
			}
		}
		if (trayMetricIndex >= trayMetrics.Count)
		{
			trayMetricIndex = 0;
		}
	}

	private void UpdateTrayText(List<FetchResult> results)
	{
		if (trayIcon == null)
		{
			return;
		}
		List<string> list = new List<string>();
		for (int i = 0; i < results.Count && i < config.channels.Count; i++)
		{
			string text = SafeText(config.channels[i].label, config.channels[i].platform);
			if (config.channels[i].benchmark && TryGetBenchmarkPair(results, i, out var mine, out var bench))
			{
				long num = bench - mine;
				list.Add(text + ((num > 0) ? " 差" : " 超") + FormatDisplayCount(Math.Abs(num)));
			}
			else if (results[i].Ok)
			{
				list.Add(text + " " + FormatDisplayCount(results[i].Count));
			}
			else if (results[i].HasCached)
			{
				list.Add(text + " " + FormatDisplayCount(results[i].CachedCount));
			}
			else
			{
				list.Add(text + " 失败");
			}
		}
		SetTrayText((list.Count == 0) ? "没有频道" : string.Join(" | ", list.ToArray()));
	}

	private void SetTrayText(string text)
	{
		if (trayIcon != null)
		{
			if (string.IsNullOrEmpty(text))
			{
				text = AppInfo.DisplayName;
			}
			else if (!text.StartsWith(AppInfo.ProductName, StringComparison.OrdinalIgnoreCase))
			{
				text = AppInfo.DisplayName + " | " + text;
			}
			if (text.Length > 63)
			{
				text = text.Substring(0, 62) + "…";
			}
			trayIcon.Text = text;
		}
	}

	private void RotateTrayIconMetric()
	{
		if (trayMetrics.Count > 1)
		{
			trayMetricIndex = (trayMetricIndex + 1) % trayMetrics.Count;
			UpdateTrayIconVisual();
		}
	}

	private void UpdateTrayIconVisual()
	{
		if (trayIcon == null)
		{
			return;
		}
		if (!config.show_tray_counts)
		{
			if (trayIconTimer != null)
			{
				trayIconTimer.Stop();
			}
			if (staticYouTubeTrayMetric != null)
			{
				SetGeneratedTrayIcon(CreateTrayDataIcon(staticYouTubeTrayMetric));
				return;
			}
			trayIcon.Icon = SystemIcons.Application;
			DisposeGeneratedTrayIcon();
			return;
		}
		if (trayMetrics.Count == 0)
		{
			if (trayIconTimer != null)
			{
				trayIconTimer.Stop();
			}
			trayIcon.Icon = SystemIcons.Application;
			DisposeGeneratedTrayIcon();
			return;
		}
		if (trayMetricIndex >= trayMetrics.Count)
		{
			trayMetricIndex = 0;
		}
		SetGeneratedTrayIcon(CreateTrayDataIcon(trayMetrics[trayMetricIndex]));
		if (trayIconTimer != null)
		{
			if (trayMetrics.Count > 1)
			{
				trayIconTimer.Start();
			}
			else
			{
				trayIconTimer.Stop();
			}
		}
	}

	private void SetGeneratedTrayIcon(Icon icon)
	{
		Icon obj = generatedTrayIcon;
		generatedTrayIcon = icon;
		trayIcon.Icon = generatedTrayIcon;
		obj?.Dispose();
	}

	private void DisposeGeneratedTrayIcon()
	{
		if (generatedTrayIcon != null)
		{
			generatedTrayIcon.Dispose();
			generatedTrayIcon = null;
		}
	}

	private Icon CreateTrayDataIcon(TrayMetric metric)
	{
		using Bitmap bitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			graphics.Clear(Color.Transparent);
			using SolidBrush brush = new SolidBrush(Theme.PanelBackground);
			using SolidBrush brush2 = new SolidBrush(metric.Accent);
			using SolidBrush brush3 = new SolidBrush(Color.White);
			using Font font = new Font("Segoe UI", (metric.Tag.Length > 1) ? 12f : 14f, FontStyle.Bold);
			using Font font2 = new Font("Segoe UI", TrayIconFontSize(metric.Count), FontStyle.Bold);
			using StringFormat stringFormat = new StringFormat();
			using GraphicsPath graphicsPath = Theme.RoundedRect(new Rectangle(0, 0, 63, 63), 14);
			stringFormat.Alignment = StringAlignment.Center;
			stringFormat.LineAlignment = StringAlignment.Center;
			graphics.FillPath(brush, graphicsPath);
			graphics.SetClip(graphicsPath);
			graphics.FillRectangle(brush2, 0, 0, 64, 22);
			graphics.ResetClip();
			graphics.DrawString(metric.Tag, font, brush3, new RectangleF(0f, 0f, 64f, 22f), stringFormat);
			graphics.DrawString(metric.Count, font2, brush3, new RectangleF(1f, 20f, 62f, 42f), stringFormat);
		}
		IntPtr hicon = bitmap.GetHicon();
		try
		{
			using Icon icon = Icon.FromHandle(hicon);
			return (Icon)icon.Clone();
		}
		finally
		{
			NativeMethods.DestroyIcon(hicon);
		}
	}

	private static float TrayIconFontSize(string text)
	{
		int num = ((!string.IsNullOrEmpty(text)) ? text.Length : 0);
		if (num <= 3)
		{
			return 22f;
		}
		if (num <= 4)
		{
			return 19f;
		}
		if (num <= 6)
		{
			return 14f;
		}
		return 11f;
	}

	private void ApplyBenchmarkDeltas(List<FetchResult> results)
	{
		if (results == null)
		{
			return;
		}
		foreach (ChannelRow row in rows)
		{
			if (!benchmarksOfTarget.TryGetValue(row.ChannelIndex, out var value))
			{
				continue;
			}
			FetchResult fetchResult = ResultAt(results, row.ChannelIndex);
			int num = -1;
			long num2 = 0L;
			long num3 = 0L;
			List<string> list = new List<string>();
			foreach (int item in value)
			{
				ChannelConfig channelConfig = config.channels[item];
				string text = SafeText(channelConfig.label, channelConfig.platform);
				if (!TryGetBenchmarkPair(results, item, out var mine, out var bench))
				{
					list.Add("◆ " + text + "：暂无数据");
					continue;
				}
				long num4 = bench - mine;
				string text2 = "◆ " + text + "：" + WidgetRules.FormatBenchmarkCount(bench) + "（" + FormatBenchmarkDeltaPercent(mine, bench) + "）";
				long? todayDelta = GetTodayDelta(item, bench);
				long? todayDelta2 = GetTodayDelta(row.ChannelIndex, mine);
				if (todayDelta.HasValue || todayDelta2.HasValue)
				{
					text2 = text2 + "\n    今日：你 " + FormatSignedCount(todayDelta2) + "，对方 " + FormatSignedCount(todayDelta);
				}
				if (lastCreatorFetch != null && lastCreatorFetch.BenchmarkLatestVideo.TryGetValue(item, out var value2))
				{
					int num5 = Math.Max(0, (DateTime.Now.Date - value2.ToLocalTime().Date).Days);
					text2 = text2 + "\n    对方上次发布 " + ((num5 == 0) ? "今天" : (num5 + " 天前")) + ((num5 >= 7) ? "，机会窗口" : "");
				}
				if (num4 > 0)
				{
					double? num6 = DailyAverageGain(row.ChannelIndex, mine);
					double? num7 = DailyAverageGain(item, bench);
					if (num6.HasValue)
					{
						double num8 = num6.Value - num7.GetValueOrDefault();
						if (num8 > 0.0)
						{
							int num9 = (int)Math.Ceiling((double)num4 / num8);
							if (num9 <= 730)
							{
								text2 = text2 + "\n    按近 7 日均速，约 " + num9 + " 天追上";
							}
						}
					}
				}
				list.Add(text2);
				num2 = mine;
				if (num < 0)
				{
					num = item;
					num3 = bench;
					continue;
				}
				bool flag = num3 > num2;
				bool flag2 = bench > num2;
				if (flag2 != flag)
				{
					if (flag2)
					{
						num = item;
						num3 = bench;
					}
				}
				else if (Math.Abs(bench - num2) < Math.Abs(num3 - num2))
				{
					num = item;
					num3 = bench;
				}
			}
			SetRowToolTip(row, string.Join("\n", list.ToArray()));
			if (num < 0)
			{
				if (row.ProgressBar != null)
				{
					row.ProgressBar.SetRatio(null);
				}
				continue;
			}
			long num10 = num3 - num2;
			ChannelConfig channelConfig2 = config.channels[num];
			long? num11 = ((fetchResult != null && fetchResult.Ok) ? GetTodayDelta(row.ChannelIndex, num2) : ((long?)null));
			string text3 = ((value.Count > 1) ? SafeText(channelConfig2.label, channelConfig2.platform) : "对标");
			row.DetailLabel.Text = ComposeBenchmarkDetail(row.DetailLabel, text3, num2, num3, num11);
			row.DetailLabel.ForeColor = ((num10 > 0) ? Theme.Warning : Theme.Success);
			if (row.ProgressBar != null)
			{
				row.ProgressBar.SetRatio((num3 <= 0) ? 1.0 : ((double)num2 / (double)num3));
			}
		}
	}

	private string ComposeBenchmarkDetail(Label label, string benchmarkName, long mine, long benchmark, long? todayDelta)
	{
		string countText = WidgetRules.FormatBenchmarkCount(benchmark);
		string percentText = FormatBenchmarkDeltaPercent(mine, benchmark);
		string primaryText = benchmarkName + " " + countText + " · " + percentText;
		List<string> candidates = new List<string>();
		if (todayDelta.HasValue && todayDelta.Value != 0L)
		{
			string sign = ((todayDelta.Value > 0L) ? "+" : "-");
			candidates.Add(primaryText + " · 今日" + sign + FormatDisplayCount(Math.Abs(todayDelta.Value)));
		}
		candidates.Add(primaryText);
		candidates.Add(benchmarkName + countText + " · " + percentText.Replace(" ", ""));
		if (!string.Equals(benchmarkName, "对标", StringComparison.Ordinal))
		{
			candidates.Add("对标" + countText + " · " + percentText.Replace(" ", ""));
		}
		candidates.Add(countText + " · " + percentText.Replace(" ", ""));
		candidates.Add(percentText.Replace(" ", ""));
		foreach (string candidate in candidates)
		{
			if (FitsSingleLine(label, candidate))
			{
				return candidate;
			}
		}
		return candidates[candidates.Count - 1];
	}

	private static bool FitsSingleLine(Label label, string text)
	{
		if (label == null || string.IsNullOrEmpty(text))
		{
			return true;
		}
		Size proposedSize = new Size(int.MaxValue, Math.Max(1, label.ClientSize.Height));
		Size measuredSize = TextRenderer.MeasureText(text, label.Font, proposedSize, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
		return measuredSize.Width <= Math.Max(1, label.ClientSize.Width - 2);
	}

	private void UpdateSparklines(List<FetchResult> results)
	{
		foreach (ChannelRow row in rows)
		{
			if (row.Card == null)
			{
				continue;
			}
			FetchResult result = ResultAt(results, row.ChannelIndex);
			if (!HasEffectiveCount(result))
			{
				row.Card.SetSparkline(null);
				continue;
			}
			ChannelConfig channelConfig = config.channels[row.ChannelIndex];
			string b = ChannelCacheKey(channelConfig);
			string b2 = NormalizePlatform(channelConfig.platform);
			List<DailyBaselineConfig> list = new List<DailyBaselineConfig>();
			foreach (DailyBaselineConfig item in config.daily_history)
			{
				if (item != null && string.Equals(item.key, b, StringComparison.OrdinalIgnoreCase) && string.Equals(item.platform, b2, StringComparison.OrdinalIgnoreCase))
				{
					list.Add(item);
				}
			}
			list.Sort((DailyBaselineConfig a, DailyBaselineConfig dailyBaselineConfig) => string.CompareOrdinal(a.date, dailyBaselineConfig.date));
			if (list.Count > 14)
			{
				list.RemoveRange(0, list.Count - 14);
			}
			List<long> list2 = new List<long>();
			string text = DateTime.Now.ToString("yyyy-MM-dd");
			foreach (DailyBaselineConfig item2 in list)
			{
				if (item2.date != text)
				{
					list2.Add(item2.count);
				}
			}
			list2.Add(EffectiveCount(result));
			row.Card.SetSparkline((list2.Count >= 3) ? list2.ToArray() : null);
		}
	}

	private void SetRowToolTip(ChannelRow row, string text)
	{
		if (row != null)
		{
			if (row.Card != null)
			{
				rowToolTip.SetToolTip(row.Card, text);
			}
			if (row.DetailLabel != null)
			{
				rowToolTip.SetToolTip(row.DetailLabel, text);
			}
			if (row.CountLabel != null)
			{
				rowToolTip.SetToolTip(row.CountLabel, text);
			}
		}
	}

	private bool TryGetBenchmarkPair(List<FetchResult> results, int benchmarkIndex, out long mine, out long bench)
	{
		mine = 0L;
		bench = 0L;
		if (results == null || benchmarkIndex < 0 || benchmarkIndex >= results.Count || benchmarkIndex >= config.channels.Count)
		{
			return false;
		}
		if (!config.channels[benchmarkIndex].benchmark || !HasEffectiveCount(results[benchmarkIndex]))
		{
			return false;
		}
		if (!targetOfBenchmark.TryGetValue(benchmarkIndex, out var value))
		{
			return false;
		}
		FetchResult result = ResultAt(results, value);
		if (!HasEffectiveCount(result))
		{
			return false;
		}
		mine = EffectiveCount(result);
		bench = EffectiveCount(results[benchmarkIndex]);
		return true;
	}

	private int FindBenchmarkTargetIndex(ChannelConfig benchmarkChannel)
	{
		if (benchmarkChannel == null)
		{
			return -1;
		}
		string text2 = (benchmarkChannel.compare_to_key ?? "").Trim();
		if (text2.Length > 0)
		{
			for (int i = 0; i < config.channels.Count; i++)
			{
				ChannelConfig channelConfig = config.channels[i];
				if (channelConfig != null && !channelConfig.benchmark && string.Equals(ChannelCacheKey(channelConfig), text2, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
		}
		string text = (benchmarkChannel.compare_to ?? "").Trim();
		if (text.Length > 0)
		{
			for (int j = 0; j < config.channels.Count; j++)
			{
				ChannelConfig channelConfig2 = config.channels[j];
				if (channelConfig2 != null && !channelConfig2.benchmark && string.Equals(SafeText(channelConfig2.label, channelConfig2.platform), text, StringComparison.OrdinalIgnoreCase))
				{
					return j;
				}
			}
		}
		string b = NormalizePlatform(benchmarkChannel.platform);
		for (int k = 0; k < config.channels.Count; k++)
		{
			ChannelConfig channelConfig3 = config.channels[k];
			if (channelConfig3 != null && !channelConfig3.benchmark && string.Equals(NormalizePlatform(channelConfig3.platform), b, StringComparison.OrdinalIgnoreCase))
			{
				return k;
			}
		}
		return -1;
	}

	private static bool HasEffectiveCount(FetchResult result)
	{
		if (result == null)
		{
			return false;
		}
		if (!result.Ok)
		{
			return result.HasCached;
		}
		return true;
	}

	private static long EffectiveCount(FetchResult result)
	{
		if (!result.Ok)
		{
			return result.CachedCount;
		}
		return result.Count;
	}

	private static string SafeText(string preferred, string fallback)
	{
		if (!string.IsNullOrEmpty(preferred))
		{
			return preferred;
		}
		if (!string.IsNullOrEmpty(fallback))
		{
			return fallback;
		}
		return "频道";
	}

	private static CachedCountConfig FindCachedCount(WidgetConfig config, ChannelConfig channel)
	{
		return ChannelIdentity.FindCachedCount(config, channel);
	}

	private static string ChannelCacheKey(ChannelConfig channel)
	{
		return ChannelIdentity.CacheKey(channel);
	}

	private static string NormalizePlatform(string platform)
	{
		return ChannelIdentity.NormalizePlatform(platform);
	}

	private static string FirstString(params string[] values)
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

	private static string FormatCachedTime(string value)
	{
		if (DateTime.TryParse(value, out var result))
		{
			return result.ToLocalTime().ToString("MM-dd HH:mm");
		}
		return "未知";
	}

	private static bool IsBilibili(string platform)
	{
		return ChannelIdentity.IsBilibili(platform);
	}

	private static string Truncate(string text, int limit)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "读取失败";
		}
		if (text.Length <= limit)
		{
			return text;
		}
		return text.Substring(0, limit - 1) + "…";
	}

	private string FormatDisplayCount(long value)
	{
		if (!config.show_full_counts)
		{
			return FormatCompactCount(value);
		}
		return FormatFullCount(value);
	}

	private string FormatTrayCount(long value)
	{
		return FormatCompactTrayCount(value);
	}

	private static string FormatFullCount(long value)
	{
		return value.ToString("N0");
	}

	private static string FormatCompactCount(long value)
	{
		if (value >= 100000000)
		{
			return ((double)value / 100000000.0).ToString("0.#") + "亿";
		}
		if (value >= 10000)
		{
			return ((double)value / 10000.0).ToString("0.#") + "万";
		}
		return value.ToString("N0");
	}

	private static string FormatCompactTrayCount(long value)
	{
		if (value >= 100000000)
		{
			return ((double)value / 100000000.0).ToString("0.#") + "y";
		}
		if (value >= 10000)
		{
			return ((double)value / 10000.0).ToString("0.#") + "w";
		}
		if (value >= 1000)
		{
			return ((double)value / 1000.0).ToString("0.#") + "k";
		}
		return value.ToString();
	}

	private static string FormatBenchmarkDeltaPercent(long mine, long benchmark)
	{
		return WidgetRules.FormatBenchmarkDeltaPercent(mine, benchmark);
	}

	private static string FormatMilestoneTarget(long value)
	{
		if (value >= 1000000000)
		{
			return ((double)value / 1000000000.0).ToString("0.#") + "B";
		}
		if (value >= 1000000)
		{
			return ((double)value / 1000000.0).ToString("0.#") + "M";
		}
		if (value >= 1000)
		{
			return ((double)value / 1000.0).ToString("0.#") + "K";
		}
		return value.ToString("N0");
	}
}
