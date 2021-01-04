using System.Collections.Generic;
using System.Windows;
using YATECommon.Settings;

namespace HBE
{
  class SetupConfigurationDataProvider
  {
    public HBECardSettings Settings { get; private set; }

    public ExpansionMain MainClass { get; private set; }

    public List<string> EPROMTypeList { get; private set; } = new List<string>()
    {
      "2K (2516, 2716)",
      "4k (2532, 2732)",
      "8K (2764)",
      "16K (27128)",
      "32K (27256)"
    };

    public SetupConfigurationDataProvider(ExpansionMain in_main_class)
    {
      MainClass = in_main_class;
      Load();
    }

    public void Load()
    {
      Settings = SettingsFile.Editing.GetSettings<HBECardSettings>(MainClass.ExpansionIndex);
    }

    public void Save()
    {
      SettingsFile.Editing.SetSettings(Settings, MainClass.ExpansionIndex);
    }
  }
}
