using HBF.Forms;
using TVCEmuCommon.ModuleManager;

namespace HBF
{
  public class ExpansionMain : ModuleBase
  {
    #region · Data members ·
    private ModuleSettingsInfo[] m_module_settings_info;
    #endregion

    public ExpansionMain()
    {
      ModuleName = GetDisplayName();
      m_module_settings_info = new ModuleSettingsInfo[]
      {
        new ModuleSettingsInfo("Config", new SetupConfiguration(), null),
        new ModuleSettingsInfo("Information", new SetupInformation(), null),
        new ModuleSettingsInfo("About", new SetupAbout(), null)
      };
    }

    override public string GetDisplayName()
    {
      return "Floppy Interface Card";
    }

    public override ModuleSettingsInfo[] GetSettingsInfo()
    {
      return m_module_settings_info;
    }

    public override string GetSettingsName()
    {
      return "HBF";
    }
  }
}
