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
using TVCEmu.Helpers;

namespace TVCHardware
{
	public class TVCKeyboard
	{
		#region · Constants ·

		public const int KeyboardRowCount = 16;

		#endregion

		#region · Types ·

		[Flags]
		private enum KeyModifiers
		{
			None,
			Shift,
			NoShift,
			AcceptShift
		};


		private class KeyMappingEntry
		{
			public Key WindowsKey;
			public ModifierKeys WindowsModifiers;


			public int Row;
			public int Column;
			public KeyModifiers Modifiers;

			public KeyMappingEntry(Key in_windows_key, ModifierKeys in_windows_modifiers, int in_row, int in_column, KeyModifiers in_modifiers)
			{
				WindowsKey = in_windows_key;
				WindowsModifiers = in_windows_modifiers;

				Row = in_row;
				Column = in_column;
				Modifiers = in_modifiers;
			}

			public override int GetHashCode()
			{
				return HashCodeHelper.Hash(WindowsKey.GetHashCode(), WindowsModifiers.GetHashCode());
			}

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

		private class KeyMappingCollection : KeyedCollection<int, KeyMappingEntry>
		{
			protected override int GetKeyForItem(KeyMappingEntry item)
			{
				return item.GetHashCode();
			}
		}

		#endregion

		#region · Data members ·

		private TVComputer m_tvc;

		private byte[] m_keyboard_matrix;

		private int m_selected_row = 0;

		private KeyMappingCollection m_key_mapping;

		#endregion

		public TVCKeyboard(TVComputer in_tvc)
		{
			m_tvc = in_tvc;

			m_keyboard_matrix = new byte[KeyboardRowCount];
			for (int i = 0; i < m_keyboard_matrix.Length; i++)
			{
				m_keyboard_matrix[i] = 0xff;
			}

			CreateKeyMappingTable();

			m_tvc.Ports.AddPortWriter(0x03, PortWrite03H);
			m_tvc.Ports.AddPortReader(0x58, PortRead58H);

		}

		private void PortRead58H(ushort in_address, ref byte inout_data)
		{
			inout_data = m_keyboard_matrix[m_selected_row];
		}

		private void PortWrite03H(ushort in_address, byte in_data)
		{
			m_selected_row = in_data & 0x0f;
		}

		public void KeyDown(KeyEventArgs in_eventargs)
		{
			Debug.WriteLine(in_eventargs.Key.ToString() + " " + Keyboard.Modifiers.ToString());


			if (in_eventargs.IsDown && !in_eventargs.IsRepeat)
			{
				KeyMappingEntry windows_key;

				if (in_eventargs.Key == Key.DeadCharProcessed)
					windows_key = new KeyMappingEntry(in_eventargs.DeadCharProcessedKey, Keyboard.Modifiers, 0, 0, KeyModifiers.None);
				else
					windows_key = new KeyMappingEntry(in_eventargs.Key, Keyboard.Modifiers, 0, 0, KeyModifiers.None);

				if (m_key_mapping.Contains(windows_key))
				{
					KeyMappingEntry entry = m_key_mapping[windows_key.GetHashCode()];

					m_keyboard_matrix[entry.Row] &= (byte)(~(1 << entry.Column));
				}
			}
		}

		public void KeyUp(KeyEventArgs in_eventargs)
		{
			if(!in_eventargs.IsDown)
			{
				KeyMappingEntry windows_key = new KeyMappingEntry(in_eventargs.Key, Keyboard.Modifiers, 0, 0, KeyModifiers.None);
				if (m_key_mapping.Contains(windows_key))
				{
					KeyMappingEntry entry = m_key_mapping[windows_key.GetHashCode()];

					m_keyboard_matrix[entry.Row] |= (byte)(1 << entry.Column);
				}
			}
		}

		/****************************************************************************
		* Keyboard matrix:
		*      b7 b6 b5 b4 b3 b2 b1 b0
		*      !  '     /  &  "  +  %
		* 0.   4  1  Í  6  0  2  3  5
		*      =        #     )  (  ~
		* 1.   7  Ö  Ó  *  ü  9  8  ^
		*            `		 $
		* 2.   R  Q  @  Z  ;  W  E  T
		*								{						}
		* 3.   U  P  Ú  [  Ő  O  I  ]
		* 					 >		 |
		* 4.   F  A  <  H  \  S  D  G
		*			
		* 5.   J  É  Ű  Re Á  L  K  De
		* 
		* 6.   V  Y  Lo N  Sh X  C  B
		* 
		* 7.   Al ,  .  Es Ct Sp _  M
		* 8.   In Up Do       Ri Le 
		* 9.   -  -  -  -  -  -  -  sh
		*
		****************************************************************************/

		private void CreateKeyMappingTable()
		{
			m_key_mapping = new KeyMappingCollection();

			// load deafult key mapping
			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = "TVCEmu.Resources.DefaultKeyMapping.txt";

			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			using (StreamReader reader = new StreamReader(stream))
			{
				while(!reader.EndOfStream)
				{
					int pos;
					string line = reader.ReadLine();

					// remove spaces
					line = line.Replace(" ", "");

					// remove comment
					pos = line.IndexOf(';');
					if (pos >= 0)
						line = line.Substring(0, pos);

					string[] fields = line.Split('|');

					if (fields.Length == 5)
					{
						Key windows_key = (Key)Enum.Parse(typeof(Key), fields[0], true);
						ModifierKeys modifier_keys = (ModifierKeys)Enum.Parse(typeof(ModifierKeys), fields[1], true);
						int row = int.Parse(fields[2]);
						int col = int.Parse(fields[3]);
						KeyModifiers key_modifiers = (KeyModifiers)Enum.Parse(typeof(KeyModifiers), fields[4], true);

						m_key_mapping.Add(new KeyMappingEntry(windows_key, modifier_keys, row, col, key_modifiers));

						if (key_modifiers.HasFlag(KeyModifiers.AcceptShift))
						{
							m_key_mapping.Add(new KeyMappingEntry(windows_key, modifier_keys | ModifierKeys.Shift, row, col, key_modifiers));
						}
					}
				}
			}


			/*

			// row 0
			m_key_mapping.Add(new KeyMappingEntry(Key.D4, ModifierKeys.None, 0, 7, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D1, ModifierKeys.None, 0, 6, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Oem102, ModifierKeys.None, 0, 5, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D6, ModifierKeys.None, 0, 4, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D0, ModifierKeys.None, 0, 3, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D2, ModifierKeys.None, 0, 2, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D3, ModifierKeys.None, 0, 1, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D5, ModifierKeys.None, 0, 0, KeyModifiers.None));

			// row 1
			m_key_mapping.Add(new KeyMappingEntry(Key.D7, ModifierKeys.None, 1, 7, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Oem3, ModifierKeys.None, 1, 6, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.OemPlus, ModifierKeys.None, 1, 5, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Multiply, ModifierKeys.None, 1, 4, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Oem2, ModifierKeys.None, 1, 3, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D9, ModifierKeys.None, 1, 2, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D8, ModifierKeys.None, 1, 1, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D3, ModifierKeys.Alt | ModifierKeys.Control, 1, 0, KeyModifiers.None));

			// row 2
			m_key_mapping.Add(new KeyMappingEntry(Key.R, ModifierKeys.None, 2, 7, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Q, ModifierKeys.None, 2, 6, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.V, ModifierKeys.Alt | ModifierKeys.Control, 2, 5, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Z, ModifierKeys.None, 2, 4, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.OemComma, ModifierKeys.Alt | ModifierKeys.Control, 2, 3, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.W, ModifierKeys.None, 2, 2, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.E, ModifierKeys.None, 2, 1, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.T, ModifierKeys.None, 2, 0, KeyModifiers.None));

			// row 3
			m_key_mapping.Add(new KeyMappingEntry(Key.U, ModifierKeys.None, 3, 7, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.P, ModifierKeys.None, 3, 6, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Oem6, ModifierKeys.None, 3, 5, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.F, ModifierKeys.Alt | ModifierKeys.Control, 3, 4, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.OemOpenBrackets, ModifierKeys.None, 3, 3, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.O, ModifierKeys.None, 3, 2, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.I, ModifierKeys.None, 3, 1, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.G, ModifierKeys.Alt | ModifierKeys.Control, 3, 0, KeyModifiers.None));

			// row 4
			m_key_mapping.Add(new KeyMappingEntry(Key.F, ModifierKeys.None, 4, 7, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.A, ModifierKeys.None, 4, 6, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.OemBackslash, ModifierKeys.Alt | ModifierKeys.Control, 4, 5, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.H, ModifierKeys.None, 4, 4, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Q, ModifierKeys.Alt | ModifierKeys.Control, 4, 3, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.S, ModifierKeys.None, 4, 2, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.D, ModifierKeys.None, 4, 1, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.G, ModifierKeys.None, 4, 0, KeyModifiers.None));

			// row 5
			m_key_mapping.Add(new KeyMappingEntry(Key.J, ModifierKeys.None, 5, 7, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Oem1, ModifierKeys.None, 5, 6, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Oem5, ModifierKeys.None, 5, 5, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Return, ModifierKeys.None, 5, 4, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.OemQuotes, ModifierKeys.None, 5, 3, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.L, ModifierKeys.None, 5, 2, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.K, ModifierKeys.None, 5, 1, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Back, ModifierKeys.None, 5, 0, KeyModifiers.None));

			// row 6
			m_key_mapping.Add(new KeyMappingEntry(Key.V, ModifierKeys.None, 6, 7, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Y, ModifierKeys.None, 6, 6, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Capital, ModifierKeys.None, 6, 5, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.N, ModifierKeys.None, 6, 4, KeyModifiers.None));
			//m_key_mapping.Add(new KeyMappingEntry(Key.OemQuotes, ModifierKeys.None, 6, 3, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.X, ModifierKeys.None, 6, 2, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.C, ModifierKeys.None, 6, 1, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.B, ModifierKeys.None, 6, 0, KeyModifiers.None));

			// row 7
			m_key_mapping.Add(new KeyMappingEntry(Key.M, ModifierKeys.None, 7, 7, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.OemMinus, ModifierKeys.None, 7, 6, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Space, ModifierKeys.None, 7, 5, KeyModifiers.None));
			//m_key_mapping.Add(new KeyMappingEntry(Key.N, ModifierKeys.None, 7, 4, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.End, ModifierKeys.None, 7, 3, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.OemPeriod, ModifierKeys.None, 7, 2, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.OemComma, ModifierKeys.None, 7, 1, KeyModifiers.None));
			//m_key_mapping.Add(new KeyMappingEntry(Key.B, ModifierKeys.None, 7, 0, KeyModifiers.None));

			// row 8
			m_key_mapping.Add(new KeyMappingEntry(Key.Left, ModifierKeys.None, 8, 6, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Right, ModifierKeys.None, 8, 5, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Down, ModifierKeys.None, 8, 2, KeyModifiers.None));
			m_key_mapping.Add(new KeyMappingEntry(Key.Up, ModifierKeys.None, 8, 1, KeyModifiers.None));
			//m_key_mapping.Add(new KeyMappingEntry(Key.B, ModifierKeys.None, 7, 0, KeyModifiers.None));
			*/
		}

	}
}
