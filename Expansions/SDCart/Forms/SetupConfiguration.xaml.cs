using CustomControls;
using System.IO;
using System.Windows;
using YATECommon;
using YATECommon.SetupPage;

namespace SDCart.Forms
{
	/// <summary>
	/// Interaction logic for SetupFiles.xaml
	/// </summary>
	public partial class SetupConfiguration : SetupPageBase
	{
    private SetupConfigurationDataProvider m_data_provider;

    public SetupConfiguration()
		{
			InitializeComponent();
		}

    public override void OnSetupPageActivating(Window in_parent, SetupPageEventArgs in_event_info)
    {
      // setup data provider
      m_data_provider = new SetupConfigurationDataProvider((ExpansionMain)in_event_info.MainClass);
      this.DataContext = m_data_provider;
    }

    public override void OnSetupPageDeactivating(Window in_parent, SetupPageEventArgs in_event_info)
    {
      m_data_provider.Save();
    }

    private void BFileSystemBrowse_Click(object sender, RoutedEventArgs e)
    {
      FolderBrowserDialog dialog = new FolderBrowserDialog()
      {
        InitialDirectory = m_data_provider.Settings.FilesystemFolder
      };

      if (dialog.ShowDialog(TVCManagers.Default.MainWindow) == true)
      {
        m_data_provider.Settings.FilesystemFolder = dialog.FileName;
      }
    }
  }
}
