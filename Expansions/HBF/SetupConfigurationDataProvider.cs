using YATECommon.Settings;

namespace HBF
{
  class SetupConfigurationDataProvider
  {
    public string[] ROMTypeList { get; private set; } = new string[] { "(custom)", "UPM", "VT-DOS 1.1", "VT-DOS 1.2" };

    public HBFCardSettings Settings { get; private set; }

    public ExpansionMain MainClass { get; private set; }

    public SetupConfigurationDataProvider(ExpansionMain in_main_class)
    {
      MainClass = in_main_class;
      Load();
    }

    public void Load()
    {
      Settings = SettingsFile.Editing.GetSettings<HBFCardSettings>(MainClass.ExpansionIndex);
    }

    public void Save()
    {
      SettingsFile.Editing.SetSettings(Settings, MainClass.ExpansionIndex);
    }
  }
}
