using System.IO;
using System.Windows;
using YATECommon.SetupPage;

namespace SoundMagic.Forms
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

    private void BROMBrowse_Click(object sender, RoutedEventArgs e)
    {
      Microsoft.Win32.OpenFileDialog dlg = CreateFileDialog();

      if (!string.IsNullOrEmpty(m_data_provider.Settings.ROMFileName))
      {
        dlg.InitialDirectory = Path.GetDirectoryName(m_data_provider.Settings.ROMFileName);
      }

      // Show open file dialog box
      bool? result = null;
      result = dlg.ShowDialog();

      // Process open file dialog box results
      if (result == true)
      {
        m_data_provider.Settings.ROMFileName = dlg.FileName;
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
