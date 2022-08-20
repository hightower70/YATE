using YATECommon.Settings;

namespace MultiCart
{
  class SetupConfigurationDataProvider
  {
    public MultiCartSettings Settings { get; private set; }

    public string[] RAMSizeList { get; private set; }

    public ExpansionMain MainClass { get; private set; }

    public SetupConfigurationDataProvider(ExpansionMain in_main_class)
    {
      MainClass = in_main_class;
      RAMSizeList = new string[] { "64k", "128k", "256k", "512k" };
      Load();
    }

    public void Load()
    {
      Settings = SettingsFile.Editing.GetSettings<MultiCartSettings>(MainClass.ExpansionIndex);
    }

    public void Save()
    {
      SettingsFile.Editing.SetSettings(Settings, MainClass.ExpansionIndex);
    }
  }
}
