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
// Data provider class for SetupForms dialog
///////////////////////////////////////////////////////////////////////////////
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using YATECommon.Drivers;

namespace YATECommon.Settings
{
  class SetupInputDataProvider : INotifyPropertyChanged
	{
		public SetupInputSettings Settings { get; private set; }
		//private Joystick m_joystick;
		public List<Joystick> InstalledJoysticks { get; private set; }
		public string[] KeyboardMappings { get; private set; }


    public event PropertyChangedEventHandler PropertyChanged;

    public bool Joystick1Left { get => m_joystick1.Left; }
    public bool Joystick1Right { get => m_joystick1.Right; }
    public bool Joystick1Up { get => m_joystick1.Up; }
    public bool Joystick1Down { get => m_joystick1.Down; }
    public bool Joystick1Fire { get => m_joystick1.Fire; }
    public bool Joystick1Acceleration { get => m_joystick1.Acceleration; }

    public bool Joystick2Left { get => m_joystick2.Left; }
    public bool Joystick2Right { get => m_joystick2.Right; }
    public bool Joystick2Up { get => m_joystick2.Up; }
    public bool Joystick2Down { get => m_joystick2.Down; }
    public bool Joystick2Fire { get => m_joystick2.Fire; }
    public bool Joystick2Acceleration { get => m_joystick2.Acceleration; }

    private TVCJoystick m_joystick1;
    private TVCJoystick m_joystick2;

    private Dictionary<string, int> m_controller_lookup;

    public SetupInputDataProvider(Window in_parent)
		{
      m_controller_lookup = new Dictionary<string, int>();
      InstalledJoysticks = Joystick.GetJoysticks();

      // create controller lookup
      m_controller_lookup.Clear();
      for (int i = 0; i < InstalledJoysticks.Count; i++)
      {
        m_controller_lookup.Add(InstalledJoysticks[i].Description, i);
      }

      m_joystick1 = new TVCJoystick();
      m_joystick2 = new TVCJoystick();

      KeyboardMappings = new string[] { "default" };

			Load();
		}

		public void Load()
		{						 
			Settings = SettingsFile.Editing.GetSettings<SetupInputSettings>();

      m_joystick1.SetSettings(Settings.Joystick1);
      m_joystick2.SetSettings(Settings.Joystick2);
		}

		public void Save()
		{
      SettingsFile.Editing.SetSettings(Settings);
		}

    public void UpdateJoystickState()
    {
      // refresh joystick 1
      int joy1_index;
      if (m_controller_lookup.TryGetValue(Settings.Joystick1.ControllerName, out joy1_index))
      {
        if (m_joystick1.JoystickDevice != null && m_joystick1.JoystickDevice.ID != joy1_index)
        {
          m_joystick1.SetSettings(Settings.Joystick1);
        }
      }

      if (m_joystick1 != null)
      {
        m_joystick1.Update();
      }

      NotifyPropertyChanged("Joystick1Left");
      NotifyPropertyChanged("Joystick1Right");
      NotifyPropertyChanged("Joystick1Up");
      NotifyPropertyChanged("Joystick1Down");
      NotifyPropertyChanged("Joystick1Fire");
      NotifyPropertyChanged("Joystick1Acceleration");

      // refresh joystick 2
      int joy2_index;
      if (m_controller_lookup.TryGetValue(Settings.Joystick2.ControllerName, out joy2_index))
      {
        if (m_joystick2.JoystickDevice != null && m_joystick2.JoystickDevice.ID != joy2_index)
        {
          m_joystick2.SetSettings(Settings.Joystick2);
        }
      }

      if (m_joystick2 != null)
      {
        m_joystick2.Update();
      }

      NotifyPropertyChanged("Joystick2Left");
      NotifyPropertyChanged("Joystick2Right");
      NotifyPropertyChanged("Joystick2Up");
      NotifyPropertyChanged("Joystick2Down");
      NotifyPropertyChanged("Joystick2Fire");
      NotifyPropertyChanged("Joystick2Acceleration");
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
