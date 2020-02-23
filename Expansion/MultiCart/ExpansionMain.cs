using MultiCart.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TVCEmuCommon.ModuleManager;

namespace MultiCart
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
			return "MultiCart ROM Extender";
		}

		public override ModuleSettingsInfo[] GetSettingsInfo()
		{
			return m_module_settings_info;
		}

		public override string GetSettingsName()
		{
			return "MultiCart";
		}
	}
}

