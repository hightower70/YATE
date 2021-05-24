using MultiCart.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YATECommon;
using YATECommon.ModuleManager;

namespace MultiCart
{
	public class ModuleMain : ModuleBase
	{
		#region · Data members ·
		private ModuleSetupPageInfo[] m_setup_page_info;
    private TVCMultiCart m_cartridge;
		#endregion

		public ModuleMain()
		{
			m_setup_page_info = new ModuleSetupPageInfo[]
			{
				new ModuleSetupPageInfo("Config", typeof( SetupConfiguration)),
				new ModuleSetupPageInfo("Information", typeof(SetupInformation)),
				new ModuleSetupPageInfo("About", typeof( SetupAbout))
			};
		}

    public override void GetModuleInfo(ModuleInfo inout_module_info)
    {
      inout_module_info.Description = "MultiCart ROM Extender";
      inout_module_info.SectionName = "MultiCart";
      inout_module_info.Type = ModuleInfo.ModuleType.Cartridge;
      inout_module_info.SetupPages = m_setup_page_info;
    }

    /// <summary>
    /// Installs this expansion to the emulated computer
    /// </summary>
    public override void Install(ITVComputer in_computer)
    {
      m_cartridge = new TVCMultiCart();
      m_cartridge.Initialize(in_computer);
      in_computer.InsertCartridge(m_cartridge);
    }

    /// <summary>
    /// Removes this expansion module from the emulated computer
    /// </summary>
    public override void Remove(ITVComputer in_computer)
    {
      in_computer.RemoveCartridge();
    }
  }
}

