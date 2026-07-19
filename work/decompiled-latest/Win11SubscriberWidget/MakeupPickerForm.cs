using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class MakeupPickerForm : Form
{
	public DateTime? SelectedDate { get; private set; }

	private MakeupPickerForm(string title, List<DateTime> candidates, bool monthly)
	{
		Text = AppInfo.DisplayName + " · " + title;
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MinimizeBox = false;
		MaximizeBox = false;
		ShowInTaskbar = false;
		AutoScaleMode = AutoScaleMode.Dpi;
		ClientSize = new Size(388, 390);
		BackColor = Theme.PanelBackground;
		ForeColor = Theme.TextPrimary;
		Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
		BuildUi(title, candidates, monthly);
	}

	public static DateTime? Pick(Form owner, string title, List<DateTime> candidates, bool monthly)
	{
		if (candidates == null || candidates.Count == 0)
		{
			MessageBox.Show("目前没有可补签的日期。", AppInfo.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return null;
		}
		using MakeupPickerForm makeupPickerForm = new MakeupPickerForm(title, candidates, monthly);
		return (makeupPickerForm.ShowDialog(owner) == DialogResult.OK) ? makeupPickerForm.SelectedDate : null;
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		NativeMethods.ApplyDarkTitleBar(Handle);
		NativeMethods.ApplyRoundedCorners(Handle);
	}

	private void BuildUi(string title, List<DateTime> candidates, bool monthly)
	{
		Label label = new Label
		{
			Text = title,
			ForeColor = Theme.TextPrimary,
			BackColor = BackColor,
			Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(18, 16)
		};
		Controls.Add(label);
		Label label2 = new Label
		{
			Text = "选择一个漏签项，随后确认消耗 1 张补签卡。",
			ForeColor = Theme.TextMuted,
			BackColor = BackColor,
			Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Regular),
			AutoSize = true,
			Location = new Point(18, 45)
		};
		Controls.Add(label2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			Location = new Point(0, 72),
			Padding = new Padding(18, 76, 10, 52),
			AutoScroll = true,
			BackColor = BackColor
		};
		Controls.Add(flowLayoutPanel);
		foreach (DateTime candidate in candidates)
		{
			DateTime date = candidate;
			Button button = new Button
			{
				Text = monthly ? date.ToString("yyyy 年 M 月") : date.ToString("M 月 d 日 dddd"),
				Width = monthly ? 108 : 112,
				Height = 34,
				Margin = new Padding(0, 0, 8, 8),
				FlatStyle = FlatStyle.Flat,
				BackColor = Theme.InputBackground,
				ForeColor = Theme.TextSecondary,
				Cursor = Cursors.Hand
			};
			button.FlatAppearance.BorderColor = Theme.CardBorder;
			button.FlatAppearance.MouseOverBackColor = Color.FromArgb(76, 58, 170);
			button.Click += delegate
			{
				SelectedDate = date;
				DialogResult = DialogResult.OK;
				Close();
			};
			flowLayoutPanel.Controls.Add(button);
		}
		Button button2 = new Button
		{
			Text = "取消",
			Width = 88,
			Height = 30,
			FlatStyle = FlatStyle.Flat,
			BackColor = Theme.InputBackground,
			ForeColor = Theme.TextSecondary,
			Location = new Point(ClientSize.Width - 106, ClientSize.Height - 39),
			Cursor = Cursors.Hand
		};
		button2.FlatAppearance.BorderColor = Theme.CardBorder;
		button2.Click += delegate
		{
			DialogResult = DialogResult.Cancel;
			Close();
		};
		Controls.Add(button2);
		label.BringToFront();
		label2.BringToFront();
		button2.BringToFront();
	}
}
