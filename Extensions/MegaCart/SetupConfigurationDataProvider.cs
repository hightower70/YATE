using YATECommon.Settings;

namespace MegaCart
{
  class SetupConfigurationDataProvider
  {
    public MegaCartSettings Settings { get; private set; }

    public ExpansionMain MainClass { get; private set; }

    public SetupConfigurationDataProvider(ExpansionMain in_main_class)
    {
      MainClass = in_main_class;
      Load();
    }

    public void Load()
    {
      Settings = SettingsFile.Editing.GetSettings<MegaCartSettings>(MainClass.ExpansionIndex);
    }

    public void Save()
    {
      SettingsFile.Editing.SetSettings(Settings, MainClass.ExpansionIndex);
    }
  }
}
