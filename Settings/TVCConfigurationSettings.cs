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
// Class for storing 'TVC Configuration' setup panel settings
///////////////////////////////////////////////////////////////////////////////
using YATECommon.Settings;

namespace YATE.Settings
{
	class TVCConfigurationSettings : SettingsBase
	{
		public int HardwareVersion { get; set; }
		public int ROMVersion { set; get; }

    public string ROMPath { get; set; }
    public string ExtROMPath { get; set; }

    /// <summary>Cartridge image file name</summary>
    public string CartridgeFileName { set; get; }
    /// <summary>True if cartridge is enabled</summary>
    public bool CartridgeActive { get; set; }

    public TVCConfigurationSettings() : base( SettingsCategory.TVC, "TVCConfiguration")
		{
			SetDefaultValues();
		}

		override public void SetDefaultValues()
		{
			HardwareVersion = 1;
			ROMVersion = 1;

      ROMPath = "";
      ExtROMPath = "";
		}
	}
}
