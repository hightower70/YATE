///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2020 Laszlo Arvai. All rights reserved.
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
// Floppy interface card settings
///////////////////////////////////////////////////////////////////////////////
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon.Settings;

namespace HBF
{
  public class HBFCardSettings : CardSettingsBase, INotifyPropertyChanged
  {
    private int m_rom_type;
    private string m_rom_file_name;
    private string m_disk_image_file_name;
    private HBFDriveSettings m_drive1_settings;
    private HBFDriveSettings m_drive2_settings;
    private HBFDriveSettings m_drive3_settings;
    private HBFDriveSettings m_drive4_settings;

    public int ROMType { get { return m_rom_type; } set { m_rom_type = value; OnPropertyChanged(); } }
    public string ROMFileName { get { return m_rom_file_name; } set { m_rom_file_name = value; OnPropertyChanged(); } }
    public string DiskImageFileName { get { return m_disk_image_file_name; } set { m_disk_image_file_name = value; OnPropertyChanged(); } }

    public HBFDriveSettings Drive1Settings { get { return m_drive1_settings; } set { m_drive1_settings = value; } }
    public HBFDriveSettings Drive2Settings { get { return m_drive2_settings; } set { m_drive2_settings = value; } }
    public HBFDriveSettings Drive3Settings { get { return m_drive3_settings; } set { m_drive3_settings = value; } }
    public HBFDriveSettings Drive4Settings { get { return m_drive4_settings; } set { m_drive4_settings = value; } }

    public HBFCardSettings() : base(SettingsCategory.TVC, ExpansionMain.ModuleName)
    {
      m_drive1_settings = new HBFDriveSettings();
      m_drive2_settings = new HBFDriveSettings();
      m_drive3_settings = new HBFDriveSettings();
      m_drive4_settings = new HBFDriveSettings();

      SetDefaultValues();
    }

    override public void SetDefaultValues()
    {
      base.SetDefaultValues();

      ROMType = 3;
      ROMFileName = "";
      DiskImageFileName = "";

      Drive1Settings.SetDefaultValues();
      Drive2Settings.SetDefaultValues();
      Drive3Settings.SetDefaultValues();
      Drive4Settings.SetDefaultValues();
    }

    #region · INotifyPropertyChanged Members ·

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
