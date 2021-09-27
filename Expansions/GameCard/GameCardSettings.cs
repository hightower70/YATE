///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2021 Laszlo Arvai. All rights reserved.
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
// GameCard configuration
///////////////////////////////////////////////////////////////////////////////
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon.Settings;

namespace GameCard
{
  public class GameCardSettings : CardSettingsBase, INotifyPropertyChanged
  {
    private string m_ROM_file_name;
    public string ROMFileName { get { return m_ROM_file_name; } set { m_ROM_file_name = value; OnPropertyChanged(); } }

    public JoystickSettings Joystick3 { get; set; }
    public JoystickSettings Joystick4 { get; set; }

    public GameCardSettings() : base(SettingsCategory.TVC, ExpansionMain.ModuleName)
    {
      Joystick3 = new JoystickSettings("Joystick3");
      Joystick4 = new JoystickSettings("Joystick4");

      SetDefaultValues();
    }

    override public void SetDefaultValues()
    {
      base.SetDefaultValues();

      Joystick3.SetDefaultValues();
      Joystick4.SetDefaultValues();

      ROMFileName = "";
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
