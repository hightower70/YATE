using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using YATECommon;
using YATECommon.Drivers;
using YATECommon.Settings;
using YATECommon.SetupPage;

namespace YATE.Dialogs
{
	/// <summary>
	/// Interaction logic for SetupForms.xaml
	/// </summary>
	public partial class SetupInput : SetupPageBase
	{
		private SetupInputDataProvider m_data_provider;
    private DispatcherTimer m_timer;

    public SetupInput()
		{
			InitializeComponent();
		}

		public override void OnSetupPageActivating(Window in_parent, SetupPageEventArgs in_event_info)
    {
      // setup data provider
      m_data_provider = new SetupInputDataProvider(in_parent.Owner);
      this.DataContext = m_data_provider;

      // start refresh timer
      m_timer = new DispatcherTimer();
      m_timer.Tick += dispatcherTimer_Tick;
      m_timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
      m_timer.Start();
    }

    public override void OnSetupPageDeactivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
      m_timer.Stop();
      m_timer = null;

			m_data_provider.Save();
		}

    private void dispatcherTimer_Tick(object sender, EventArgs e)
    {
      m_data_provider.UpdateJoystickState();
    }
  }
}
