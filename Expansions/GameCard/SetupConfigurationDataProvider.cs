using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon;
using YATECommon.Drivers;
using YATECommon.Settings;

namespace GameCard
{
  class SetupConfigurationDataProvider : INotifyPropertyChanged
  {
    public GameCardSettings Settings { get; private set; }

    public ExpansionMain MainClass { get; private set; }

    public List<Joystick> InstalledJoysticks { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;

    public bool Joystick3Left { get => m_joystick3.Left; }
    public bool Joystick3Right { get => m_joystick3.Right; }
    public bool Joystick3Up { get => m_joystick3.Up; }
    public bool Joystick3Down { get => m_joystick3.Down; }
    public bool Joystick3Fire { get => m_joystick3.Fire; }
    public bool Joystick3Acceleration { get => m_joystick3.Acceleration; }

    public bool Joystick4Left { get => m_joystick4.Left; }
    public bool Joystick4Right { get => m_joystick4.Right; }
    public bool Joystick4Up { get => m_joystick4.Up; }
    public bool Joystick4Down { get => m_joystick4.Down; }
    public bool Joystick4Fire { get => m_joystick4.Fire; }
    public bool Joystick4Acceleration { get => m_joystick4.Acceleration; }

    private TVCJoystick m_joystick3;
    private TVCJoystick m_joystick4;

    private Dictionary<string, int> m_controller_lookup;


    public SetupConfigurationDataProvider(ExpansionMain in_main_class)
    {
      m_controller_lookup = new Dictionary<string, int>();

      InstalledJoysticks = Joystick.GetJoysticks();

      // create controller lookup
      m_controller_lookup.Clear();
      for (int i = 0; i < InstalledJoysticks.Count; i++)
      {
        m_controller_lookup.Add(InstalledJoysticks[i].Description, i);
      }

      m_joystick3 = new TVCJoystick();
      m_joystick4 = new TVCJoystick();

      MainClass = in_main_class;
      Load();

    }

    public void Load()
    {
      Settings = SettingsFile.Editing.GetSettings<GameCardSettings>(MainClass.ExpansionIndex);
    }

    public void Save()
    {
      SettingsFile.Editing.SetSettings(Settings, MainClass.ExpansionIndex);
    }

    public void UpdateJoystickState()
    {
      // refresh joystick 3
      int joy3_index;
      if (m_controller_lookup.TryGetValue(Settings.Joystick3.ControllerName, out joy3_index))
      {
        if (m_joystick3.JoystickDevice == null || m_joystick3.JoystickDevice.ID != joy3_index)
        {
          m_joystick3.SetSettings(Settings.Joystick3);
        }
      }

      m_joystick3.Update();

      NotifyPropertyChanged("Joystick3Left");
      NotifyPropertyChanged("Joystick3Right");
      NotifyPropertyChanged("Joystick3Up");
      NotifyPropertyChanged("Joystick3Down");
      NotifyPropertyChanged("Joystick3Fire");
      NotifyPropertyChanged("Joystick3Acceleration");

      // refresh joystick 4
      int joy4_index;
      if (m_controller_lookup.TryGetValue(Settings.Joystick4.ControllerName, out joy4_index))
      {
        if (m_joystick4.JoystickDevice == null || m_joystick4.JoystickDevice.ID != joy4_index)
        {
          m_joystick4.SetSettings(Settings.Joystick4);
        }
      }

      m_joystick4.Update();

      NotifyPropertyChanged("Joystick4Left");
      NotifyPropertyChanged("Joystick4Right");
      NotifyPropertyChanged("Joystick4Up");
      NotifyPropertyChanged("Joystick4Down");
      NotifyPropertyChanged("Joystick4Fire");
      NotifyPropertyChanged("Joystick4Acceleration");
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
