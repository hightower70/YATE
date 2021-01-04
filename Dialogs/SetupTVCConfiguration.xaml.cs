using System.Windows;
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

	}
}
