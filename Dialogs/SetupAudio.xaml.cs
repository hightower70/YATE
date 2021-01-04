using System.Windows;
using YATE.Drivers;
using YATE.Settings;
using YATECommon.Settings;
using YATECommon.SetupPage;

namespace YATE.Dialogs
{
  /// <summary>
  /// Interaction logic for SetupForms.xaml
  /// </summary>
  public partial class SetupAudio : SetupPageBase
	{
    public SetupAudioSettings Settings { get; private set; }
    public WaveOut.WaveOutDeviceInfo [] AudioOutDevices { get; private set; }

    public SetupAudio()
		{
			InitializeComponent();
		}

		public override void OnSetupPageActivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
      // setup data provider
      Settings = SettingsFile.Editing.GetSettings<SetupAudioSettings>();
      AudioOutDevices = WaveOut.GetWaveOutDevices();
      this.DataContext = this;
		}

		public override void OnSetupPageDeactivating(Window in_parent, SetupPageEventArgs in_event_info)
		{
      SettingsFile.Editing.SetSettings(Settings);
    }
  }
}
