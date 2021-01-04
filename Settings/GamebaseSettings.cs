///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2020 Laszlo Arvai. All rights reserved.
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
// Gamebase configuration data 
///////////////////////////////////////////////////////////////////////////////
using YATECommon.Settings;

namespace YATE.Settings
{
  /// <summary>
  /// Gamebase configuration settings
  /// </summary>
  public class GamebaseSettings : SettingsBase
  {
		/// <summary>Gamebase database file path</summary>
		public string GamebaseDatabaseFile { set; get; }

    /// <summary>Autostart of the loaded gamebase game</summary>
    public bool Autostart { get; set; }

    /// <summary>Position of the browse dialog</summary>
    public WindowPosSettings BrowseDialogPos { get; private set; }

    public GamebaseSettings()	: base(SettingsCategory.Emulator, "Gamebase")
		{
      BrowseDialogPos = new WindowPosSettings();
      SetDefaultValues();
		}

		override public void SetDefaultValues()
		{
			GamebaseDatabaseFile = "";
      Autostart = true;
      BrowseDialogPos.SetDefault(770, 535);
    }
	}

}
