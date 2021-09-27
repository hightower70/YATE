using CustomControls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using YATECommon;
using YATECommon.SetupPage;

namespace GameCard.Forms
{
  /// <summary>
  /// Interaction logic for SetupFiles.xaml
  /// </summary>
  public partial class SetupJoystick : SetupPageBase
  {
    private SetupConfigurationDataProvider m_data_provider;
    private DispatcherTimer m_timer;

    public SetupJoystick()
    {
      InitializeComponent();
    }

    public override void OnSetupPageActivating(Window in_parent, SetupPageEventArgs in_event_info)
    {
      // setup data provider
      m_data_provider = new SetupConfigurationDataProvider((ExpansionMain)in_event_info.MainClass);
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
