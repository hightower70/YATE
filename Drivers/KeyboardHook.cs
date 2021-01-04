using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace YATE.Drivers
{
	/// <summary>
	/// Keyboard hook class for capturing system keys
	/// </summary>
	public class KeyboardHook
	{
		#region · Constants ·
		private const int WH_KEYBOARD_LL = 13;
		private const int WM_KEYDOWN = 0x0100;
		#endregion

		#region · Delegates	·
		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
		public delegate void KeyDownProc(object sender, System.Windows.Input.KeyEventArgs e);
		#endregion

		#region · Data members ·
		private IntPtr m_hook_id = IntPtr.Zero;
		private LowLevelKeyboardProc m_hook_proc = null;
		private KeyDownProc m_key_down_proc = null;
		#endregion

		/// <summary>
		/// Enables keyboard hook
		/// </summary>
		/// <param name="in_key_down_proc">Key down event handler</param>
		public void EnableHook(KeyDownProc in_key_down_proc)
		{
			// hook only once
			if (m_hook_id != IntPtr.Zero)
				return;

			m_hook_proc = new LowLevelKeyboardProc(HookCallback);
			m_key_down_proc = in_key_down_proc;

			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule)
			{
				m_hook_id = SetWindowsHookEx(WH_KEYBOARD_LL, m_hook_proc, GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		/// <summary>
		/// Removes keyboard hook
		/// </summary>
		public void ReleaseHook()
		{
			if (m_hook_id == IntPtr.Zero)
				return;

			UnhookWindowsHookEx(m_hook_id);

			m_hook_proc = null;
		}

		/// <summary>
		/// Internal keyboard hook procedure
		/// </summary>
		/// <param name="nCode"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
			{
				bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
				bool control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

				int forms_key = Marshal.ReadInt32(lParam);

				Key key = KeyInterop.KeyFromVirtualKey(forms_key);

				if (key == Key.Escape && control)
				{
					//KeyEventArgs e = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, key);
					KeyEventArgs e = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, key)
					{
						RoutedEvent = Keyboard.PreviewKeyDownEvent
					};

					m_key_down_proc?.Invoke(this, e);

					return (IntPtr)1;
				}
			}

			return CallNextHookEx(m_hook_id, nCode, wParam, lParam);
		}

		#region · Native functions ·

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		#endregion
	}
}