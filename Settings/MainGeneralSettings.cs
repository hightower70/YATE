using System.IO;
using System.Reflection;

namespace YATECommon.Settings
{
  public class MainGeneralSettings : SettingsBase
  {
		// Path settings
		public string ModulesPath { set; get; }

		// Main window settings
		public WindowPosSettings MainWindowPos;
		
		public MainGeneralSettings()	: base(SettingsCategory.Emulator, "General")
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
