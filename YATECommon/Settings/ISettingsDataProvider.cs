namespace YATECommon.Settings
{
  public interface ISettingsDataProvider
	{
		void LoadSettings(SettingsFile in_settings);
		void SaveSettings(SettingsFile in_settings);
	}
}
