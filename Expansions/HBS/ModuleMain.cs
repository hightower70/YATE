using HBS.Forms;
using YATECommon;
using YATECommon.ModuleManager;

namespace HBS
{
  public class ModuleMain : ModuleBase
  {
    #region · Data members ·
    private ModuleSetupPageInfo[] m_settings_page_info;
    #endregion

    public ModuleMain()
    {
      m_settings_page_info = new ModuleSetupPageInfo[]
      {
        new ModuleSetupPageInfo("Config", typeof(SetupConfiguration)),
        new ModuleSetupPageInfo("Information", typeof(SetupInformation)),
        new ModuleSetupPageInfo("About", typeof(SetupAbout))
      };
    }

    public override void Initialize(ModuleManager in_expansion_manager)
    {
      base.Initialize(in_expansion_manager);

      //Settings = ParentManager.Settings.GetSettings<HBSCardSettings>();
    }


    public override void GetModuleInfo(ModuleInfo inout_module_info)
    {
      inout_module_info.Description = "Serial Interface Card";
      inout_module_info.SectionName = "HBS";
      inout_module_info.Type = ModuleInfo.ModuleType.Card;
      inout_module_info.SetupPages = m_settings_page_info;
    }

    /// <summary>
    /// Installs this expansion to the emulated computer
    /// </summary>
    public override void Install(ITVComputer in_computer)
    {
    }

    /// <summary>
    /// Removes this expansion module from the emulated computer
    /// </summary>
    public override void Remove(ITVComputer in_computer)
    {
    }
  }
}
