﻿///////////////////////////////////////////////////////////////////////////////
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

namespace YATE.Settings
{
  class SetupTVCConfigurationDataProvider
  {
    public const int TVCHardwareVersion32k = 0;
    public const int TVCHardwareVersion64k = 1;
    public const int TVCHardwareVersion64kPaging = 2;
    public const int TVCHardwareVersion64kplus = 3;

    public const int TVCROMCustom = 0;
    public const int TVCROM1_2 = 1;
    public const int TVCROM1_2_RU = 2;
    public const int TVCROM2_1 = 3;
    public const int TVCROM2_2 = 4;

    public TVCConfigurationSettings Settings { get; private set; }

    public string[] ComputerVersionList { get; private set; }
    public string[] HardwareVersionList { get; private set; }
    public string[] ROMVersionList { get; private set; }

    public SetupTVCConfigurationDataProvider(Window in_parent)
    {
      //m_joystick = new Joystick(in_parent);
      //InstalledJoysticks = m_joystick.FindJoysticks();

      ComputerVersionList = new string[] { "TV Computer", "TV Computer 64k", "TV Computer 64k+", "TV Computer 64k (U3->U0 paging mod.)", "TV Computer 64k (Russian)", "Custom" };
      HardwareVersionList = new string[] { "TVC 32k", "TVC 64k", "TVC 64 & Ram Paging", "TVC 64k+" };
      ROMVersionList = new string[] { "(custom)", "BASIC 1.2", "BASIC 1.2 (RU)", "BASIC 2.1", "BASIC 2.2" };

      Load();
    }

    public void Load()
    {
      Settings = SettingsFile.Editing.GetSettings<TVCConfigurationSettings>();
    }

    public void Save()
    {
      SettingsFile.Editing.SetSettings(Settings);
    }
  }
}

