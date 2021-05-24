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
// SDCart configuration
///////////////////////////////////////////////////////////////////////////////
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon.Settings;

namespace SDCart
{
  public class SDCartSettings : ExpansionSettingsBase, INotifyPropertyChanged
  {
    private string m_filesystem_folder;

    public string FilesystemFolder { get { return m_filesystem_folder; } set { m_filesystem_folder = value; OnPropertyChanged(); } }
    
    public SDCartSettings() : base(SettingsCategory.TVC, ExpansionMain.ModuleName)
    {
      SetDefaultValues();
    }

    override public void SetDefaultValues()
    {
      base.SetDefaultValues();

      FilesystemFolder = "";
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
