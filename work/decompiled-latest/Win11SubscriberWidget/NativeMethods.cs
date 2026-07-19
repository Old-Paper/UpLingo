using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Win11SubscriberWidget;

internal static class NativeMethods
{
	public const uint SWP_NOZORDER = 4u;

	public const uint SWP_NOACTIVATE = 16u;

	public const uint SWP_SHOWWINDOW = 64u;

	public const uint SWP_FRAMECHANGED = 32u;

	private const int GWL_STYLE = -16;

	private const long WS_CHILD = 1073741824L;

	private const long WS_POPUP = 2147483648L;

	private const long WS_VISIBLE = 268435456L;

	private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

	private const int DWMWCP_ROUND = 2;

	private const int EM_SETCUEBANNER = 5377;

	[StructLayout(LayoutKind.Sequential)]
	private struct LASTINPUTINFO
	{
		public uint cbSize;

		public uint dwTime;
	}

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool DestroyIcon(IntPtr hIcon);

	[DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
	private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
	private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
	private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
	private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

	public static void SetTextBoxPlaceholder(TextBoxBase textBox, string placeholder)
	{
		try
		{
			SendMessage(textBox.Handle, 5377, new IntPtr(1), placeholder);
		}
		catch (Exception ex)
		{
			AppLogger.Error("textbox-placeholder", ex);
		}
	}

	public static int ForegroundProcessId()
	{
		IntPtr foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == IntPtr.Zero)
		{
			return 0;
		}
		GetWindowThreadProcessId(foregroundWindow, out var processId);
		return (int)processId;
	}

	public static bool HasRecentUserInput(int maximumIdleSeconds)
	{
		LASTINPUTINFO info = new LASTINPUTINFO
		{
			cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO))
		};
		if (!GetLastInputInfo(ref info))
		{
			return true;
		}
		uint idleMilliseconds = unchecked((uint)Environment.TickCount - info.dwTime);
		return idleMilliseconds <= (uint)Math.Max(1, maximumIdleSeconds) * 1000u;
	}

	public static void ApplyRoundedCorners(IntPtr hwnd)
	{
		try
		{
			int value = 2;
			DwmSetWindowAttribute(hwnd, 33, ref value, 4);
		}
		catch (Exception ex)
		{
			AppLogger.Error("rounded-corners", ex);
		}
	}

	public static void ApplyDarkTitleBar(IntPtr hwnd)
	{
		try
		{
			int value = 1;
			DwmSetWindowAttribute(hwnd, 20, ref value, 4);
		}
		catch (Exception ex)
		{
			AppLogger.Error("dark-titlebar", ex);
		}
	}

	public static void SetWindowAsPopup(IntPtr hwnd)
	{
		long windowStyle = GetWindowStyle(hwnd);
		windowStyle = (windowStyle | 0x80000000u | 0x10000000) & -1073741825;
		SetWindowStyle(hwnd, windowStyle);
	}

	private static long GetWindowStyle(IntPtr hwnd)
	{
		return ((IntPtr.Size == 8) ? GetWindowLongPtr64(hwnd, -16) : GetWindowLong32(hwnd, -16)).ToInt64();
	}

	private static void SetWindowStyle(IntPtr hwnd, long style)
	{
		IntPtr dwNewLong = new IntPtr(style);
		if (IntPtr.Size == 8)
		{
			SetWindowLongPtr64(hwnd, -16, dwNewLong);
		}
		else
		{
			SetWindowLong32(hwnd, -16, dwNewLong);
		}
	}
}
