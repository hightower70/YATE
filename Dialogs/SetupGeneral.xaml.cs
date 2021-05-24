﻿using System.Windows;
using YATE.Settings;
using YATECommon.Settings;
using YATECommon.SetupPage;

namespace YATE.Dialogs
{
	/// <summary>
	/// Interaction logic for SetupPath.xaml
	/// </summary>
	public partial class SetupGeneral : SetupPageBase
	{
		private MainGeneralSettings m_data_provider;

		public SetupGeneral()
		{
			InitializeComponent();
		}

		public override void OnSetupPageActivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
			// setup data provider
			m_data_provider = SettingsFile.Editing.GetSettings<MainGeneralSettings>();
			this.DataContext = m_data_provider;
		}

		public override void OnSetupPageDeactivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
      SettingsFile.Editing.SetSettings(m_data_provider);
		}

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }
  }
}
