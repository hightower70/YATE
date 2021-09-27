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

      bool custom = combobox.SelectedIndex == 4;

      cbHardwareVersion.IsEnabled = custom;
      cbROMVersion.IsEnabled = custom;

      switch(combobox.SelectedIndex)
      {
        // TVC 32k
        case 0:
          cbHardwareVersion.SelectedIndex = 0;
          cbROMVersion.SelectedIndex = 1;
          break;

        // TVC 64k
        case 1:
          cbHardwareVersion.SelectedIndex = 1;
          cbROMVersion.SelectedIndex = 1;
          break;
        
        // TVC 64k+
        case 2:
          cbHardwareVersion.SelectedIndex = 3;
          cbROMVersion.SelectedIndex = 4;
          break;

        // TVC 64k (RU)
        case 3:
          cbHardwareVersion.SelectedIndex = 1;
          cbROMVersion.SelectedIndex = 2;
          break;

        // Custom
        case 4:
          break;
      }

    }
  }
}
