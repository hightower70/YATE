///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2013-2014 Laszlo Arvai. All rights reserved.
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
// Joystick configuration data
///////////////////////////////////////////////////////////////////////////////

using YATECommon.Drivers;

namespace YATECommon.Settings
{
  public class JoystickSettings : SettingsBase
  {
    public string ControllerName { get; set; }

    public int LeftChannel { get; set; }
    public int LeftThreshold { get; set; }

    public int RightChannel { get; set; }
    public int RightThreshold { get; set; }

    public int UpChannel { get; set; }
    public int UpThreshold { get; set; }

    public int DownChannel { get; set; }
    public int DownThreshold { get; set; }

    public int FireChannel { get; set; }
    public int FireThreshold { get; set; }

    public int AccelerationChannel { get; set; }
    public int AccelerationThreshold { get; set; }

    public JoystickSettings(string in_joystick_settings_name) : base(SettingsCategory.Emulator, in_joystick_settings_name)
    {
      SetDefaultValues();
    }

    override public void SetDefaultValues()
    {
      ControllerName = "[none]";

      LeftChannel = (int)JoystickChannel.XM;
      LeftThreshold = 10;

      RightChannel = (int)JoystickChannel.XP;
      RightThreshold = 10;

      UpChannel = (int)JoystickChannel.YM;
      UpThreshold = 10;

      DownChannel = (int)JoystickChannel.YP;
      DownThreshold = 10;

      FireChannel = (int)JoystickChannel.Button1;
      FireThreshold = 10;

      AccelerationChannel = (int)JoystickChannel.Button2;
      AccelerationThreshold = 10;
    }
  }
}
