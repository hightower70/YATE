using System.Reflection;
using HBF.Forms;
using TVCEmuCommon;
using TVCEmuCommon.ExpansionManager;

namespace HBF
{
  public class ExpansionMain : ExpansionBase
  {
    #region · Data members ·
    private ExpansionSetupPageInfo[] m_settings_page_info;
    private HBFCard m_floppy_card;
    #endregion

    public ExpansionMain()
    {
      m_settings_page_info = new ExpansionSetupPageInfo[]
      {
        new ExpansionSetupPageInfo("Config", typeof(SetupConfiguration)),
        new ExpansionSetupPageInfo("Information", typeof( SetupInformation)),
        new ExpansionSetupPageInfo("About", typeof( SetupAbout))
      };
    }

    public override void GetExpansionInfo(ExpansionInfo inout_module_info)
    {
      inout_module_info.Description = "Floppy Interface Card";
      inout_module_info.SectionName = "HBF";
      inout_module_info.Type = ExpansionInfo.ExpansionType.Card;
      inout_module_info.SetupPages = m_settings_page_info;
    }

    public override void Initialize(ExpansionManager in_expansion_manager)
    {
      base.Initialize(in_expansion_manager);

      Settings = ParentManager.Settings.GetSettings<HBFCardSettings>();
    }

    public override void Install(ITVComputer in_computer)
    {
      m_floppy_card = new HBFCard();
      m_floppy_card.SetSettings((HBFCardSettings)Settings);
      in_computer.InsertCard(((HBFCardSettings)Settings).SlotIndex, m_floppy_card);
    }
  }
}
