///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019 Laszlo Arvai. All rights reserved.
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
// Videoton TV Computer Keyboard Emulation
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using TVCEmuCommon.Helpers;

namespace TVCHardware
{
	/// <summary>
	/// TVC keyboard hardware emulation
	/// </summary>
	public class TVCKeyboard
	{
		#region · Constants ·

		public const int KeyboardRowCount = 16;		// Number of keyboard row in hardware
		private const int PressedKeyCount = 16;   // Number of simulataniously pressed keys handled by the system
		private const int KeyboardInjectionRate = 40; // injected string rate key/ms

		#endregion

		#region · Types ·

		/// <summary>
		/// Requested modifiers for the given key combination on the TVC keyboard hardware
		/// </summary>
		[Flags]
		private enum KeyModifiers
		{
			None				= 0,

			RemoveShift	= 0x0001,
			AddShift		= 0x0002,
			KeepShift		= 0x0004,

			RemoveCtrl	= 0x0010,
			AddCtrl			= 0x0020,
			KeepCtrl		= 0x0040,

			RemoveAlt		= 0x0100,
			AddAlt			= 0x0200,
			KeepAlt			= 0x0400,

			RemoveAll   = RemoveShift | RemoveCtrl | RemoveAlt,
			KeepAll			= KeepShift | KeepCtrl | KeepAlt
		};

		/// <summary>
		/// Storage of one mapped (Windows->TVC) key
		/// </summary>
		private class KeyMappingEntry
		{
			/// <summary>Windows key code</summary>
			public Key WindowsKey;
			/// <summary> Windows modifier code</summary>
			public ModifierKeys WindowsModifiers;

			/// <summary>TVC key row</summary>
			public int Row;
			/// <summary> TVC key column</summary>
			public int Column;
			/// <summary> TVC required modifiers</summary>
			public KeyModifiers Modifiers;

			/// <summary>
			/// Construct key mapping entry
			/// </summary>
			/// <param name="in_windows_key">Windows key code</param>
			/// <param name="in_windows_modifiers">Windows modifier</param>
			/// <param name="in_row">TVC key row</param>
			/// <param name="in_column">TVC key column</param>
			/// <param name="in_modifiers">TVC modifiers</param>
			public KeyMappingEntry(Key in_windows_key, ModifierKeys in_windows_modifiers, int in_row, int in_column, KeyModifiers in_modifiers)
			{
				WindowsKey = in_windows_key;
				WindowsModifiers = in_windows_modifiers;

				Row = in_row;
				Column = in_column;
				Modifiers = in_modifiers;
			}

			/// <summary>
			/// Construct entry for storing windows key information only (used for lookup)
			/// </summary>
			/// <param name="in_windows_key">Windows key code</param>
			/// <param name="in_windows_modifiers">Windows key modifier</param>
			public KeyMappingEntry(Key in_windows_key, ModifierKeys in_windows_modifiers)
			{
				WindowsKey = in_windows_key;
				WindowsModifiers = in_windows_modifiers;

				Row = 0;
				Column = 0;
				Modifiers = KeyModifiers.None;
			}

			/// <summary>
			/// gets has code for lookup
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				return HashCodeHelper.Hash(WindowsKey.GetHashCode(), WindowsModifiers.GetHashCode());
			}

			/// <summary>
			/// Checks for equiality of windows keys
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public override bool Equals(object obj)
			{
				if (obj is KeyMappingEntry)
				{
					KeyMappingEntry entry = (KeyMappingEntry)obj;

					return (entry.WindowsKey == WindowsKey) && (entry.WindowsModifiers == WindowsModifiers);
				}
				else
				{
					return false;
				}
			}
		}
														 
		/// <summary>
		/// Collection of key mappings (Windows->TVC)
		/// </summary>
		private class KeyMappingCollection : KeyedCollection<int, KeyMappingEntry>
		{
			/// <summary>
			/// Gets has code of the given key
			/// </summary>
			/// <param name="item"></param>
			/// <returns></returns>
			protected override int GetKeyForItem(KeyMappingEntry item)
			{
				return item.GetHashCode();
			}
		}

		#endregion

		#region · Data members ·

		// TVC hardware class
		private TVComputer m_tvc;

		// Current TVC matrix state
		private byte[] m_keyboard_matrix;

		// currently pressed key on windows
		private Key[] m_pressed_keys;

		// TVC hardware selected rows
		private int m_selected_row = 0;

		// Key mapping table (Windows->TVC)
		private KeyMappingCollection m_key_mapping;

		// Cached modifier keys from the key mapping table
		private KeyMappingEntry m_shift_key = null;
		private KeyMappingEntry m_ctrl_key = null;
		private KeyMappingEntry m_alt_key = null;

		// Keyboard string injection members
		private string m_keyboard_injection_string;
		private int m_keyboard_injection_pos;
		private ulong m_keyboard_injection_timestamp;
		private ulong m_keyboard_injection_rate;


		#endregion

		/// <summary>
		/// Creates TVC keyboard emulation 
		/// </summary>
		/// <param name="in_tvc">TVC hardware which owns this keyboard</param>
		public TVCKeyboard(TVComputer in_tvc)
		{
			m_tvc = in_tvc;

			// init matrx
			m_keyboard_matrix = new byte[KeyboardRowCount];
			for (int i = 0; i < m_keyboard_matrix.Length; i++)
			{
				m_keyboard_matrix[i] = 0xff;
			}

			// create key mapping table
			LoadKeyMappingTableFromResource("TVCEmu.Resources.DefaultKeyMapping.txt");

			// clear pressed key table
			m_pressed_keys = new Key[PressedKeyCount];
			for (int i = 0; i < PressedKeyCount; i++)
			{
				m_pressed_keys[i] = Key.None;
			}

			// add port access handlers
			m_tvc.Ports.AddPortWriter(0x03, PortWrite03H);
			m_tvc.Ports.AddPortReader(0x58, PortRead58H);

			// setup injection service
			m_keyboard_injection_pos = -1;
			m_keyboard_injection_rate = m_tvc.MillisecToCPUTicks(KeyboardInjectionRate);
		}

		/// <summary>
		/// TVC hardware keyboard data port reader function
		/// </summary>
		/// <param name="in_address">Address of the port</param>
		/// <param name="inout_data">Data from the port</param>
		private void PortRead58H(ushort in_address, ref byte inout_data)
		{
			// update injected keys (if injection is active)
			if (m_keyboard_injection_pos >= 0)
			{
				UpdateInjectedKeyboardMatrix();
			}

			// get matrix data
			inout_data = m_keyboard_matrix[m_selected_row];
		}

		/// <summary>
		/// TVC hardware keyboard row select register
		/// </summary>
		/// <param name="in_address">Address of the register</param>
		/// <param name="in_data">Row selection data</param>
		private void PortWrite03H(ushort in_address, byte in_data)
		{
			m_selected_row = in_data & 0x0f;
		}

		public void InjectKeys(string in_string_to_inject)
		{
			m_keyboard_injection_string = in_string_to_inject;
			m_keyboard_injection_pos = 0;
			m_keyboard_injection_timestamp = m_tvc.GetCPUTicks();

			// reset keyboard matrix
			for (int i = 0; i < m_keyboard_matrix.Length; i++)
			{
				m_keyboard_matrix[i] = 0xff;
			}
		}

		/// <summary>
		/// Windows key down event handler
		/// </summary>
		/// <param name="in_eventargs">Key down event argument</param>
		public void KeyDown(KeyEventArgs in_eventargs)
		{
			in_eventargs.Handled = true;

			if (in_eventargs.RoutedEvent == Keyboard.PreviewKeyDownEvent && !in_eventargs.IsRepeat)
			{
				// determine key
				Key key = in_eventargs.Key;
				if (key == Key.DeadCharProcessed)
					key = in_eventargs.DeadCharProcessedKey;

				if (key == Key.System)
					key = in_eventargs.SystemKey;

				// check if this key is already in the list
				bool found = false;
				for (int i = 0; i < PressedKeyCount; i++)
				{
					if (m_pressed_keys[i] == key)
					{
						found = true;
					}
				}

				// not found in the pressed list -> add to it
				if (!found)
				{
					int j = -1;

					// find empty slot in the pressed key array and store the pressed key
					for (int i = 0; i < PressedKeyCount; i++)
					{
						if (m_pressed_keys[i] == Key.None)
						{
							m_pressed_keys[i] = key;
							j = i;
							break;
						}
					}

#if DEBUG_TVC_KEYBOARD
					Debug.Write("D: " + key.ToString() + " " + Keyboard.Modifiers.ToString() + " [" + j.ToString() + "] M:");
#endif
					// update TVC keyboard
					UpdateKeyboardMatrix();
				}
			}
		}

		/// <summary>
		/// Windows key up event handler
		/// </summary>
		/// <param name="in_eventargs">Key up event handler</param>
		public void KeyUp(KeyEventArgs in_eventargs)
		{
			in_eventargs.Handled = true;

			if (!in_eventargs.IsDown)
			{
				// determine key
				Key key = in_eventargs.Key;
				if (key == Key.DeadCharProcessed)
					key = in_eventargs.DeadCharProcessedKey;

				if (key == Key.System)
					key = in_eventargs.SystemKey;

#if DEBUG_TVC_KEYBOARD
				Debug.Write("U: " + key.ToString() + " " + Keyboard.Modifiers.ToString() + " M:");
#endif
				// delete this key from the store of the pressed key
				int i;
				for (i = 0; i < PressedKeyCount; i++)
				{
					if (m_pressed_keys[i] == key)
					{
						m_pressed_keys[i] = Key.None;
					}
				}

				// update TVC keyboard
				UpdateKeyboardMatrix();
			}
		}

		/// <summary>
		/// Update TVC hardware keyboard matrix content
		/// </summary>
		private void UpdateKeyboardMatrix()
		{
			// do not update when injection is in progress
			if (m_keyboard_injection_pos >= 0)
			{
				return;
			}

			////////////////////////
			// init keyboard matrix
			byte[] keyboard_matrix = new byte[KeyboardRowCount];
			for (int i = 0; i < keyboard_matrix.Length; i++)
			{
				keyboard_matrix[i] = 0xff;
			}

			if (m_keyboard_injection_pos >= 0)
			{
				Key pressed_key;

				if (m_tvc.GetTicksSince(m_keyboard_injection_timestamp) > m_keyboard_injection_rate)
				{
					m_keyboard_injection_timestamp = m_tvc.GetCPUTicks();
					m_keyboard_injection_pos++;
					if (m_keyboard_injection_pos >= m_keyboard_injection_string.Length)
					{
						m_keyboard_injection_pos = -1;
					}
				}

				if (m_keyboard_injection_pos >= 0)
				{
					pressed_key = (Key)Enum.Parse(typeof(Key), m_keyboard_injection_string.Substring(m_keyboard_injection_pos, 1), true);

					KeyMappingEntry windows_key = new KeyMappingEntry(pressed_key, ModifierKeys.None, 0, 0, KeyModifiers.None);

					if (m_key_mapping.Contains(windows_key))
					{
						KeyMappingEntry entry = m_key_mapping[windows_key.GetHashCode()];

						keyboard_matrix[entry.Row] &= (byte)(~(1 << entry.Column));
					}
				}
			}
			else
			{

				/////////////////////////////////
				// Add pressed keys to the matrix
				KeyModifiers modifiers = KeyModifiers.KeepAll;

				for (int i = 0; i < PressedKeyCount; i++)
				{
					if (m_pressed_keys[i] != Key.None)
					{
						KeyMappingEntry windows_key = new KeyMappingEntry(m_pressed_keys[i], Keyboard.Modifiers, 0, 0, KeyModifiers.None);

						if (m_key_mapping.Contains(windows_key))
						{
							KeyMappingEntry entry = m_key_mapping[windows_key.GetHashCode()];

							keyboard_matrix[entry.Row] &= (byte)(~(1 << entry.Column));
							modifiers = entry.Modifiers;
						}
					}
				}

				////////////////////
				// Handle modifiers

				// shift key
				if (m_shift_key != null)
				{
					if ((modifiers & KeyModifiers.AddShift) != 0)
						keyboard_matrix[m_shift_key.Row] &= (byte)(~(1 << m_shift_key.Column));

					if ((modifiers & KeyModifiers.RemoveShift) != 0)
						keyboard_matrix[m_shift_key.Row] |= (byte)(1 << m_shift_key.Column);
				}

				// Ctrl shift key
				if (m_ctrl_key != null)
				{
					if ((modifiers & KeyModifiers.AddCtrl) != 0)
						keyboard_matrix[m_ctrl_key.Row] &= (byte)(~(1 << m_ctrl_key.Column));

					if ((modifiers & KeyModifiers.RemoveCtrl) != 0)
						keyboard_matrix[m_ctrl_key.Row] |= (byte)(1 << m_ctrl_key.Column);
				}

				// Alt shift key
				if (m_alt_key != null)
				{
					if ((modifiers & KeyModifiers.AddAlt) != 0)
						keyboard_matrix[m_alt_key.Row] &= (byte)(~(1 << m_alt_key.Column));

					if ((modifiers & KeyModifiers.RemoveAlt) != 0)
						keyboard_matrix[m_alt_key.Row] |= (byte)(1 << m_alt_key.Column);
				}
			}

			////////////////////////
			// store keyboard matrix					 
			for (int i = 0; i < KeyboardRowCount; i++)
			{
				m_keyboard_matrix[i] = keyboard_matrix[i];
#if DEBUG_TVC_KEYBOARD
				Debug.Write(m_keyboard_matrix[i].ToString("x2")+" ");
#endif
			}
#if DEBUG_TVC_KEYBOARD
			Debug.WriteLine("");
#endif
		}

		/// <summary>
		/// Updates injected key, advaces to the nxt character with the desired rate
		/// </summary>
		private void UpdateInjectedKeyboardMatrix()
		{
			// do nothing if injection is not active 
			if (m_keyboard_injection_pos < 0)
				return;

			// handle injection character rate
			if (m_tvc.GetTicksSince(m_keyboard_injection_timestamp) < m_keyboard_injection_rate)
				return;

			// update ticks
			m_keyboard_injection_timestamp = m_tvc.GetCPUTicks();

			// check for end of the string
			if (m_keyboard_injection_pos >= m_keyboard_injection_string.Length)
			{
				// reset keyboard matrix
				for (int i = 0; i < m_keyboard_matrix.Length; i++)
				{
					m_keyboard_matrix[i] = 0xff;
				}

				// exit injection mode
				m_keyboard_injection_pos = -1;
			}
			else
			{
				bool wait = false;

				while (m_keyboard_injection_pos >= 0 && m_keyboard_injection_pos < m_keyboard_injection_string.Length && !wait)
				{
					// get the next command and character to inject
					int char_pos = m_keyboard_injection_string.IndexOf(',', m_keyboard_injection_pos);

					char command;
					string key;

					if (char_pos >= 0)
					{
						command = m_keyboard_injection_string[m_keyboard_injection_pos];
						key = m_keyboard_injection_string.Substring(m_keyboard_injection_pos + 1, char_pos - m_keyboard_injection_pos - 1);
						m_keyboard_injection_pos = char_pos + 1;
					}
					else
					{
						command = m_keyboard_injection_string[m_keyboard_injection_pos];
						key = m_keyboard_injection_string.Substring(m_keyboard_injection_pos + 1);
						m_keyboard_injection_pos = m_keyboard_injection_string.Length;
					}

					// find keyboard entry for the command's key
					KeyMappingEntry key_map_entry = null;

					if (!string.IsNullOrEmpty(key))
					{
						Key key_code = (Key)Enum.Parse(typeof(Key), key, true);

						KeyMappingEntry windows_key = new KeyMappingEntry(key_code, ModifierKeys.None, 0, 0, KeyModifiers.None);

						if (m_key_mapping.Contains(windows_key))
						{
							key_map_entry = m_key_mapping[windows_key.GetHashCode()];
						}
					}

					// determine command
					switch (char.ToUpper(command))
					{
						// down
						case 'D':
							if (key_map_entry != null)
								m_keyboard_matrix[key_map_entry.Row] &= (byte)(~(1 << key_map_entry.Column));
							break;

						// up
						case 'U':
							if (key_map_entry != null)
								m_keyboard_matrix[key_map_entry.Row] |= (byte)(1 << key_map_entry.Column);
							break;

						// wait
						case 'W':
							wait = true;
							break;
					}
				}
			}
		}

		/// <summary>
		/// Loads key mapping from the resource text file
		/// </summary>
		/// <param name="in_resource_name">Name of the resource</param>
		public void LoadKeyMappingTableFromResource(string in_resource_name)
		{
			// load default key mapping
			var assembly = Assembly.GetExecutingAssembly();

			using (Stream stream = assembly.GetManifestResourceStream(in_resource_name))
			{
				LoadKeyMappingTable(stream);
			}
		}

		/// <summary>
		/// Loads key mappting table from text file from the specified stream
		/// </summary>
		/// <param name="in_stream">Stream conatining the key mapping text file</param>
		public void LoadKeyMappingTable(Stream in_stream)
		{
			KeyMappingCollection key_mapping = new KeyMappingCollection();

			using (StreamReader reader = new StreamReader(in_stream))
			{
				while (!reader.EndOfStream)
				{
					int pos;
					string line = reader.ReadLine();

					// remove spaces
					line = line.Replace(" ", "");
					line = line.Replace("\t", "");

					// remove comment
					pos = line.IndexOf(';');
					if (pos >= 0)
						line = line.Substring(0, pos);

					// create keyboard entry
					string[] fields = line.Split('|');

					if (fields.Length == 5)
					{
						Key windows_key = (Key)Enum.Parse(typeof(Key), fields[0], true);
						ModifierKeys modifier_keys = (ModifierKeys)Enum.Parse(typeof(ModifierKeys), fields[1], true);
						int row = int.Parse(fields[2]);
						int col = int.Parse(fields[3]);
						KeyModifiers key_modifiers = (KeyModifiers)Enum.Parse(typeof(KeyModifiers), fields[4], true);

						key_mapping.Add(new KeyMappingEntry(windows_key, modifier_keys, row, col, key_modifiers));

            // Add Shift + key
						if (key_modifiers.HasFlag(KeyModifiers.KeepShift))
						{
							key_mapping.Add(new KeyMappingEntry(windows_key, modifier_keys | ModifierKeys.Shift, row, col, key_modifiers));
						}

            // Add Ctrl + key
            if (key_modifiers.HasFlag(KeyModifiers.KeepCtrl))
            {
              key_mapping.Add(new KeyMappingEntry(windows_key, modifier_keys | ModifierKeys.Control, row, col, key_modifiers));
            }

            // Add Alt + key
            if (key_modifiers.HasFlag(KeyModifiers.KeepAlt))
            {
              key_mapping.Add(new KeyMappingEntry(windows_key, modifier_keys | ModifierKeys.Alt, row, col, key_modifiers));
            }
          }
        }
			}

			// cache control keys
			m_shift_key = null;
			m_ctrl_key = null;
			m_alt_key = null;

			KeyMappingEntry shift_key = new KeyMappingEntry(Key.LeftShift, ModifierKeys.Shift);
			if (key_mapping.Contains(shift_key))
				m_shift_key = key_mapping[shift_key.GetHashCode()];

			KeyMappingEntry ctrl_key = new KeyMappingEntry(Key.LeftCtrl, ModifierKeys.Control);
			if (key_mapping.Contains(ctrl_key))
				m_ctrl_key = key_mapping[ctrl_key.GetHashCode()];

			KeyMappingEntry alt_key = new KeyMappingEntry(Key.LeftAlt, ModifierKeys.Alt);
			if (key_mapping.Contains(alt_key))
				m_ctrl_key = key_mapping[alt_key.GetHashCode()];

			m_key_mapping = key_mapping;
		}
	}
}
