using System.IO;
using System.Reflection;
using TVCEmuCommon.Settings;

namespace TVCEmu.Settings
{
	public class MainGeneralSettings : BaseSettings
	{
		// Path settings
		public string ModulesPath { set; get; }

		// Main window settings
		public WindowPosSettings MainWindowPos;
		
		public MainGeneralSettings()	: base("Main","General")
		{
			MainWindowPos = new WindowPosSettings();
			SetDefaultValues();
		}

		override public void SetDefaultValues()
		{
			MainWindowPos.SetDefault(800, 600);

			string application_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			ModulesPath = application_path;
		}
	}

}
