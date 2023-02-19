///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2022 Laszlo Arvai. All rights reserved.
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
// MultiCart configuration
///////////////////////////////////////////////////////////////////////////////
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon.Settings;

namespace MultiCart
{
  public class MultiCartSettings : ExpansionSettingsBase, INotifyPropertyChanged
  {
    private string m_ROM1_file_name;
    private string m_ROM2_file_name;
    private int m_RAM_size;
    private bool m_autosave_flash_content;

    public string ROM1FileName { get { return m_ROM1_file_name; } set { m_ROM1_file_name = value; OnPropertyChanged(); } }
    public string ROM2FileName { get { return m_ROM2_file_name; } set { m_ROM2_file_name = value; OnPropertyChanged(); } }
    public bool AutosaveFlashContent { get { return m_autosave_flash_content; } set { m_autosave_flash_content = value; OnPropertyChanged(); } }
    public int RAMSize { get { return m_RAM_size; } set { m_RAM_size = value; OnPropertyChanged(); } }

    public MultiCartSettings() : base(SettingsCategory.TVC, ExpansionMain.ModuleName)
    {
      SetDefaultValues();
    }

    override public void SetDefaultValues()
    {
      base.SetDefaultValues();

      ROM1FileName = "";
      ROM2FileName = "";

      m_autosave_flash_content = true;

      RAMSize = 3;
    }

    #region · INotifyPropertyChanged Members ·

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string  propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
