using System.Windows;
using TVCEmuCommon.Settings;
using TVCEmuCommon.SetupPage;

namespace TVCEmu.Dialogs
{
	/// <summary>
	/// Interaction logic for SetupForms.xaml
	/// </summary>
	public partial class SetupInput : SetupPageBase
	{
		private SetupInputDataProvider m_data_provider;

		public SetupInput()
		{
			InitializeComponent();
		}

		public override void OnSetupPageActivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
			// setup data provider
			m_data_provider = new SetupInputDataProvider(in_parent.Owner);
			this.DataContext = m_data_provider;
		}

		public override void OnSetupPageDeactivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
			m_data_provider.Save();
		}
	}
}
