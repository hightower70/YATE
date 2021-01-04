using System.Reflection;
using HBF.Forms;
using YATECommon;
using YATECommon.ModuleManager;

namespace HBF
{
  public class ModuleMain : ModuleBase
  {
    #region · Data members ·
    private ModuleSetupPageInfo[] m_settings_page_info;
    private HBFCard m_floppy_card;
    #endregion

    public ModuleMain()
    {
      m_settings_page_info = new ModuleSetupPageInfo[]
      {
        new ModuleSetupPageInfo("Config", typeof(SetupConfiguration)),
        new ModuleSetupPageInfo("Information", typeof( SetupInformation)),
        new ModuleSetupPageInfo("About", typeof( SetupAbout))
      };
    }

    public override void GetModuleInfo(ModuleInfo inout_module_info)
    {
      inout_module_info.Description = "Floppy Interface Card";
      inout_module_info.SectionName = "HBF";
      inout_module_info.Type = ModuleInfo.ModuleType.Card;
      inout_module_info.SetupPages = m_settings_page_info;
    }

    public override void Initialize(ModuleManager in_expansion_manager)
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

    /// <summary>
    /// Removes this expansion module from the emulated computer
    /// </summary>
    public override void Remove(ITVComputer in_computer)
    {
      in_computer.RemoveCard(((HBFCardSettings)Settings).SlotIndex);
    }
  }
}
