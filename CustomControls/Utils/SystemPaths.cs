using System;
using System.IO;

namespace CustomControls.Utils
{
	public class SystemPaths
	{
		/// <summary>
		/// Gets application data path (usually: "users\user.name\Application Data\ApplicationName")
		/// </summary>
		/// <returns></returns>
		static public string GetApplicationDataPath()
		{
			string user_directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string executable_name = GetExecutableName();
			string application_data_path = Path.Combine(user_directory, executable_name);

			if (!Directory.Exists(application_data_path))
			{
				Directory.CreateDirectory(application_data_path);
			}

			return application_data_path;
		}

		/// <summary>
		/// Gets main executable file name (without path and extension)
		/// </summary>
		/// <returns></returns>
		static public string GetExecutableName()
		{
			return Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location);
		}

	}
}
