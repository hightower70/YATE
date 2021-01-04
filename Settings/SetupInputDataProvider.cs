///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2013 Laszlo Arvai. All rights reserved.
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
// Data provider class for SetupForms dialog
///////////////////////////////////////////////////////////////////////////////
using System.Windows;
using YATECommon.Settings;

namespace YATECommon.Settings
{
  class SetupInputDataProvider
	{
		public SetupInputSettings Settings { get; private set; }
		//private Joystick m_joystick;
		public readonly string[] InstalledJoysticks;
		public string[] KeyboardMappings { get; private set; }

		public SetupInputDataProvider(Window in_parent)
		{
			//m_joystick = new Joystick(in_parent);
			//InstalledJoysticks = m_joystick.FindJoysticks();

			KeyboardMappings = new string[] { "default" };

			Load();
		}

		public void Load()
		{						 
			Settings = SettingsFile.Editing.GetSettings<SetupInputSettings>();
		}

		public void Save()
		{
      SettingsFile.Editing.SetSettings(Settings);
		}
	}
}
