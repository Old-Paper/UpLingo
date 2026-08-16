using System;
using System.Drawing;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal sealed class ChannelEditForm : Form
{
	private readonly bool bilibili;

	private readonly TextBox valueBox;

	public string ChannelValue { get; private set; }

	public ChannelEditForm(bool isBilibili, string currentValue)
	{
		bilibili = isBilibili;
		Text = AppInfo.DisplayName + " · 编辑" + (bilibili ? "B站频道" : "YouTube 频道");
		Icon = AppIcon.Load();
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MinimizeBox = false;
		MaximizeBox = false;
		ShowInTaskbar = false;
		AutoScaleMode = AutoScaleMode.Dpi;
		ClientSize = new Size(430, 174);
		BackColor = Theme.PanelBackground;
		ForeColor = Theme.TextPrimary;
		Font = new Font("Microsoft YaHei UI", 9f);

		Label titleLabel = new Label
		{
			Text = bilibili ? "B 站 UID" : "YouTube 频道",
			ForeColor = Theme.TextPrimary,
			Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(20, 18)
		};
		Controls.Add(titleLabel);

		valueBox = new TextBox
		{
			Text = ChannelInputValidator.IsPlaceholder(currentValue) ? "" : (currentValue ?? "").Trim(),
			BackColor = Theme.InputBackground,
			ForeColor = Theme.TextPrimary,
			BorderStyle = BorderStyle.FixedSingle,
			Location = new Point(20, 48),
			Size = new Size(390, 25)
		};
		Controls.Add(valueBox);

		Label hintLabel = new Label
		{
			Text = bilibili ? "请输入个人空间地址中的纯数字 UID。" : "支持 @handle、UC 开头的频道 ID 或频道链接。",
			ForeColor = Theme.TextMuted,
			AutoSize = true,
			Location = new Point(20, 82)
		};
		Controls.Add(hintLabel);

		Button saveButton = CreateButton("保存", primary: true, new Point(222, 124));
		saveButton.Click += delegate { SaveValue(); };
		Controls.Add(saveButton);
		Button cancelButton = CreateButton("取消", primary: false, new Point(318, 124));
		cancelButton.DialogResult = DialogResult.Cancel;
		Controls.Add(cancelButton);
		AcceptButton = saveButton;
		CancelButton = cancelButton;
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		NativeMethods.ApplyDarkTitleBar(Handle);
		NativeMethods.ApplyRoundedCorners(Handle);
	}

	private void SaveValue()
	{
		string value = (valueBox.Text ?? "").Trim();
		string error = bilibili ? ChannelInputValidator.ValidateBilibili(value) : ChannelInputValidator.ValidateYouTube(value);
		if (!string.IsNullOrEmpty(error))
		{
			MessageBox.Show(error, AppInfo.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			valueBox.Focus();
			return;
		}
		ChannelValue = value;
		DialogResult = DialogResult.OK;
		Close();
	}

	private static Button CreateButton(string text, bool primary, Point location)
	{
		Button button = new Button
		{
			Text = text,
			Location = location,
			Size = new Size(92, 32),
			FlatStyle = FlatStyle.Flat,
			Cursor = Cursors.Hand,
			BackColor = primary ? Theme.BiliAccent : Theme.InputBackground,
			ForeColor = primary ? Color.White : Theme.TextSecondary
		};
		button.FlatAppearance.BorderSize = primary ? 0 : 1;
		button.FlatAppearance.BorderColor = Theme.CardBorder;
		return button;
	}
}

internal static class ChannelInputValidator
{
	public static bool IsPlaceholder(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return true;
		}
		string text = value.Trim();
		return text.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ||
			text.IndexOf("your_handle", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static string ValidateBilibili(string value)
	{
		if (IsPlaceholder(value))
		{
			return "请先填写 B 站 UID。";
		}
		for (int i = 0; i < value.Length; i++)
		{
			if (!char.IsDigit(value[i]))
			{
				return "B 站 UID 只能包含数字。";
			}
		}
		return "";
	}

	public static string ValidateYouTube(string value)
	{
		if (IsPlaceholder(value))
		{
			return "请先填写 YouTube 频道。";
		}
		if (value.StartsWith("@", StringComparison.Ordinal) || value.StartsWith("UC", StringComparison.Ordinal))
		{
			return "";
		}
		if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return "YouTube 频道应填写 @handle、UC 开头的频道 ID 或完整频道链接。";
		}
		if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) || !IsYouTubeHost(uri.Host))
		{
			return "请输入有效的 YouTube 频道链接。";
		}
		string[] parts = uri.AbsolutePath.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		bool handlePath = parts.Length > 0 && parts[0].StartsWith("@", StringComparison.Ordinal) && parts[0].Length > 1;
		bool idPath = parts.Length > 1 && (string.Equals(parts[0], "channel", StringComparison.OrdinalIgnoreCase) || string.Equals(parts[0], "user", StringComparison.OrdinalIgnoreCase));
		if (!handlePath && !idPath)
		{
			return "链接应为 YouTube 的 @handle、/channel/ 或 /user/ 频道地址。";
		}
		return "";
	}

	private static bool IsYouTubeHost(string host)
	{
		return string.Equals(host, "youtube.com", StringComparison.OrdinalIgnoreCase) ||
			host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase);
	}
}
