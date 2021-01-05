using System.IO;
using System.Windows;
using YATECommon.SetupPage;

namespace MultiCart.Forms
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

    private void BROM1Browse_Click(object sender, RoutedEventArgs e)
    {
      Microsoft.Win32.OpenFileDialog dlg = CreateFileDialog();

      if (!string.IsNullOrEmpty(m_data_provider.Settings.ROM1FileName))
      {
        dlg.InitialDirectory = Path.GetDirectoryName(m_data_provider.Settings.ROM1FileName);
      }

      // Show open file dialog box
      bool? result = null;
      result = dlg.ShowDialog();

      // Process open file dialog box results
      if (result == true)
      {
        m_data_provider.Settings.ROM1FileName = dlg.FileName;
      }
    }

    private void BROM2Browse_Click(object sender, RoutedEventArgs e)
    {
      Microsoft.Win32.OpenFileDialog dlg = CreateFileDialog();

      if (!string.IsNullOrEmpty(m_data_provider.Settings.ROM2FileName))
      {
        dlg.InitialDirectory = Path.GetDirectoryName(m_data_provider.Settings.ROM2FileName);
      }

      // Show open file dialog box
      bool? result = null;
      result = dlg.ShowDialog();

      // Process open file dialog box results
      if (result == true)
      {
        m_data_provider.Settings.ROM2FileName = dlg.FileName;
      }
    }

    private Microsoft.Win32.OpenFileDialog CreateFileDialog()
    {
      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
      {
        Filter = "Binary File (*.bin)|*.bin| All Files (*.*)|*.*"
      };

      return dlg;
    }
  }
}
