using System.Windows;
using System.Windows.Controls;
using YATECommon.SetupPage;
using static YATECommon.SetupPage.SetupPageBase;

namespace HBF.Forms
{
  /// <summary>
  /// Interaction logic for DriveDiskSettings.xaml
  /// </summary>
  public partial class SetupDiskDrive1 : SetupPageBase
  {
    private SetupConfigurationDataProvider m_data_provider;

    public SetupDiskDrive1()
    {
      InitializeComponent();
      
    }

    public override void OnSetupPageActivating(Window in_parent, SetupPageEventArgs in_event_info)
    {
      // setup data provider
      m_data_provider = new SetupConfigurationDataProvider((ExpansionMain)in_event_info.MainClass);
      DiskDriveSetup.DataContext = m_data_provider.Settings.Drive1Settings;
    }

    public override void OnSetupPageDeactivating(Window in_parent, SetupPageEventArgs in_event_info)
    {
      m_data_provider.Save();
    }
  }
}
