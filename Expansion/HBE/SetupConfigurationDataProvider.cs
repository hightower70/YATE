using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TVCEmuCommon.Settings;

namespace HBE
{
  class SetupConfigurationDataProvider
  {
    public HBECardSettings Settings { get; private set; }
    public SlotSelectionDataProvider SlotSettings { get; private set; }

    public SetupConfigurationDataProvider(Window in_parent)
    {
      Load();
    }

    public void Load()
    {
      Settings = SettingsFile.Editing.GetSettings<HBECardSettings>();
      SlotSettings = new SlotSelectionDataProvider(SettingsFile.Editing, Settings);
    }

    public void Save()
    {
      SettingsFile.Editing.SetSettings(Settings);
    }
  }
}
