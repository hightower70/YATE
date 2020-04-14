using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVCEmuCommon.Settings
{
	public interface ISettingsDataProvider
	{
		void LoadSettings(SettingsFile in_settings);
		void SaveSettings(SettingsFile in_settings);
	}
}
