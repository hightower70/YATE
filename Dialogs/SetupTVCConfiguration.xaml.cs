using System.Windows;
using System.Windows.Controls;
using YATE.Settings;
using YATECommon.Settings;
using YATECommon.SetupPage;

namespace YATE.Dialogs
{
	/// <summary>
	/// Interaction logic for SetupPath.xaml
	/// </summary>
	public partial class SetupTVCConfiguration: SetupPageBase
	{
		private SetupTVCConfigurationDataProvider m_data_provider;

		public SetupTVCConfiguration()
		{
			InitializeComponent();
		}

		public override void OnSetupPageActivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
      // setup data provider
      m_data_provider = new SetupTVCConfigurationDataProvider(in_parent.Owner);
      this.DataContext = m_data_provider;
		}

		public override void OnSetupPageDeactivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
      m_data_provider.Save();
    }

    private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      ComboBox combobox = (ComboBox)sender;

      bool custom = false;

      switch(combobox.SelectedIndex)
      {
        // TVC 32k
        case 0:
          cbHardwareVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCHardwareVersion32k;
          cbROMVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCROM1_2;
          break;

        // TVC 64k
        case 1:
          cbHardwareVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCHardwareVersion64k;
          cbROMVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCROM1_2;
          break;
        
        // TVC 64k+
        case 2:
          cbHardwareVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCHardwareVersion64kplus;
          cbROMVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCROM2_2;
          break;

        // TVC 64k (paging mod)
        case 3:
          cbHardwareVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCHardwareVersion64kPaging;
          cbROMVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCROM1_2;
          break;

        // TVC 64k (RU)
        case 4:
          cbHardwareVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCHardwareVersion64k;
          cbROMVersion.SelectedIndex = SetupTVCConfigurationDataProvider.TVCROM1_2_RU;
          break;

        // Custom
        case 5:
          custom = true;
          break;
      }

      cbHardwareVersion.IsEnabled = custom;
      cbROMVersion.IsEnabled = custom;
    }
  }
}
