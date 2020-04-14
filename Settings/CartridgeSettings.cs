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
// Cofiguration information for cartridge 
///////////////////////////////////////////////////////////////////////////////
using TVCEmuCommon.Settings;

namespace TVCEmu.Settings
{
  public class CartridgeSettings : BaseSettings
	{
    /// <summary>Cartridge image file name</summary>
    public string CartridgeFileName { set; get; }
    /// <summary>True if cartridge is enabled</summary>
    public bool CartridgeActive { get; set; }
		
		public CartridgeSettings()	: base("Main","Cartridge")
		{
			SetDefaultValues();
		}

		override public void SetDefaultValues()
		{
			CartridgeFileName = "";
      CartridgeActive = false;
		}
	}

}
