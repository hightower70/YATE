using MultiCart.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TVCEmuCommon.ExpansionManager;

namespace MultiCart
{
	public class ExpansionMain : ExpansionBase
	{
		#region · Data members ·
		private ExpansionSetupPageInfo[] m_setup_page_info;
		#endregion

		public ExpansionMain()
		{
			m_setup_page_info = new ExpansionSetupPageInfo[]
			{
				new ExpansionSetupPageInfo("Config", typeof( SetupConfiguration)),
				new ExpansionSetupPageInfo("Information", typeof(SetupInformation)),
				new ExpansionSetupPageInfo("About", typeof( SetupAbout))
			};
		}

    public override void GetExpansionInfo(ExpansionInfo inout_module_info)
    {
      inout_module_info.Description = "MultiCart ROM Extender";
      inout_module_info.SectionName = "MultiCart";
      inout_module_info.Type = ExpansionInfo.ExpansionType.Cartridge;
      inout_module_info.SetupPages = m_setup_page_info;
    }

	}
}

