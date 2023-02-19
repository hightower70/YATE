///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2013-2023 Laszlo Arvai. All rights reserved.
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
// MultiCart configuration data provider
///////////////////////////////////////////////////////////////////////////////
using YATECommon.Settings;

namespace MultiCart
{
  class SetupConfigurationDataProvider
  {
    public MultiCartSettings Settings { get; private set; }

    public string[] RAMSizeList { get; private set; }

    public ExpansionMain MainClass { get; private set; }

    public SetupConfigurationDataProvider(ExpansionMain in_main_class)
    {
      MainClass = in_main_class;
      RAMSizeList = new string[] { "64k", "128k", "256k", "512k" };
      Load();
    }

    public void Load()
    {
      Settings = SettingsFile.Editing.GetSettings<MultiCartSettings>(MainClass.ExpansionIndex);
    }

    public void Save()
    {
      SettingsFile.Editing.SetSettings(Settings, MainClass.ExpansionIndex);
    }
  }
}
