﻿///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2021 Laszlo Arvai. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
// MA 02110-1301  USA
///////////////////////////////////////////////////////////////////////////////
// File description
// ----------------
// Keyboard low level hook for capturing system keys
///////////////////////////////////////////////////////////////////////////////
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
    private const int WM_KEYUP = 0x0101;
    #endregion

    #region · Delegates	·
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
		public delegate void KeyProc(object sender, Key key, ModifierKeys modifiers);
		#endregion

		#region · Data members ·
		private IntPtr m_hook_id = IntPtr.Zero;
		private LowLevelKeyboardProc m_hook_proc = null;
    #endregion

    #region · Properties members ·
    public KeyProc OnKeyDown { get; set; } = null;
    public KeyProc OnKeyUp { get; set; } = null;
    #endregion

    #region · Public members ·

    /// <summary>
    /// Enables keyboard hook
    /// </summary>
    /// <param name="in_key_down_proc">Key down event handler</param>
    public void EnableHook()
		{
			// hook only once
			if (m_hook_id != IntPtr.Zero)
				return;

			m_hook_proc = new LowLevelKeyboardProc(HookCallback);

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

    #endregion

    #region · Private members ·

    /// <summary>
    /// Internal keyboard hook procedure
    /// </summary>
    /// <param name="nCode"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
      if (nCode >= 0)
      {
        bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
        bool control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

        if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP)
        {
          int forms_key = Marshal.ReadInt32(lParam);

          Key key = KeyInterop.KeyFromVirtualKey(forms_key);

          if (wParam == (IntPtr)WM_KEYDOWN && key == Key.Escape && control)
          {
            OnKeyDown?.Invoke(this, key, Keyboard.Modifiers);

            return (IntPtr)1;
          }

          if (wParam == (IntPtr)WM_KEYUP && key == Key.Escape)
          {
            OnKeyUp?.Invoke(this, key, Keyboard.Modifiers);
          }
        }
      }

      return CallNextHookEx(m_hook_id, nCode, wParam, lParam);
    }
    #endregion

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