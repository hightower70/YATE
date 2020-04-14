using HBS.Forms;
using TVCEmuCommon.ExpansionManager;

namespace HBS
{
  public class ExpansionMain : ExpansionBase
  {
    #region · Data members ·
    private ExpansionSetupPageInfo[] m_settings_page_info;
    #endregion

    public ExpansionMain()
    {
      m_settings_page_info = new ExpansionSetupPageInfo[]
      {
        new ExpansionSetupPageInfo("Config", typeof(SetupConfiguration)),
        new ExpansionSetupPageInfo("Information", typeof(SetupInformation)),
        new ExpansionSetupPageInfo("About", typeof(SetupAbout))
      };
    }

    public override void Initialize(ExpansionManager in_expansion_manager)
    {
      base.Initialize(in_expansion_manager);

      //Settings = ParentManager.Settings.GetSettings<HBSCardSettings>();
    }


    public override void GetExpansionInfo(ExpansionInfo inout_module_info)
    {
      inout_module_info.Description = "Serial Interface Card";
      inout_module_info.SectionName = "HBS";
      inout_module_info.Type = ExpansionInfo.ExpansionType.Card;
      inout_module_info.SetupPages = m_settings_page_info;
    }
  }
}
