using System.IO;
using System.Windows;
using YATE.Settings;
using YATECommon.Settings;
using YATECommon.SetupPage;

namespace YATE.Dialogs
{
	/// <summary>
	/// Interaction logic for SetupGamebase.xaml
	/// </summary>
	public partial class SetupGamebase : SetupPageBase
	{
		private GamebaseSettings m_data_provider;

		public SetupGamebase()
		{
			InitializeComponent();
		}

		public override void OnSetupPageActivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
			// setup data provider
			m_data_provider = SettingsFile.Editing.GetSettings<GamebaseSettings>();
			this.DataContext = m_data_provider;
		}

		public override void OnSetupPageDeactivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
      SettingsFile.Editing.SetSettings(m_data_provider);
		}

    private void bBrowse_Click(object sender, RoutedEventArgs e)
    {
      // Configure open file dialog box
      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
      {
        DefaultExt = ".mdb",
        Filter = "Gamebase Database File (*.mdb)|*.MDB"
      };

      if (m_data_provider != null && !string.IsNullOrEmpty(m_data_provider.GamebaseDatabaseFile))
      {
        dlg.InitialDirectory = Path.GetDirectoryName(m_data_provider.GamebaseDatabaseFile);
      }

      // Show open file dialog box
      bool? result = null;
      result = dlg.ShowDialog();

      // Process open file dialog box results
      if (result == true)
      {
        string file_name = dlg.FileName;

        m_data_provider.GamebaseDatabaseFile = file_name;
      }
    }
  }
}
