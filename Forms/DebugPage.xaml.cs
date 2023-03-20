using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using YATE.Managers;
using YATE.Emulator.TVCHardware;
using System.Globalization;
using YATE.Settings;
using YATECommon.Settings;

namespace YATE.Forms
{
  /// <summary>
  /// Interaction logic for RegisterPage.xaml
  /// </summary>
  public partial class DebugPage : UserControl, INotifyPropertyChanged
	{
		public static DependencyProperty ExecutionControlProperty = DependencyProperty.Register("ExecutionControl", typeof(ExecutionManager), typeof(DebugPage), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnExecutionControlPropertyChanged)));

		private static void OnExecutionControlPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((DebugPage)sender).RegisterDebugEventHandler((ExecutionManager)e.NewValue);
		}

		public ExecutionManager ExecutionControl
		{
			get { return (ExecutionManager)GetValue(ExecutionControlProperty); }
			set { SetValue(ExecutionControlProperty, value); }
		}

		private void RegisterDebugEventHandler(ExecutionManager in_execution_control)
		{
			in_execution_control.DebuggerPeriodicEvent += DebuggerBreakEventDelegate;
			m_execution_control = in_execution_control;
			DataContext = this;
		}

		private ExecutionManager m_execution_control;

		private void DebuggerBreakEventDelegate(TVComputer in_sender)
		{

    }

    public DebugPage()
		{
			InitializeComponent();
		}

	
		#region · INotifyPropertyChangedEvent ·

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

    #endregion

    private void BChange_Click(object sender, RoutedEventArgs e)
    {
      BreakpointSetForm dialog = new BreakpointSetForm();

      dialog.BreakpointAddress = ExecutionControl.BreakpointAddress;

      if(dialog.ShowDialog() ?? false)
      {
        ExecutionControl.BreakpointAddress = dialog.BreakpointAddress;
        UpdateBreakpointAddress();

        DebugSettings settings = SettingsFile.Default.GetSettings<DebugSettings>();

        settings.BreakpointAddress = dialog.BreakpointAddress;

        SettingsFile.Default.SetSettings(settings);
        SettingsFile.Default.Save();
      }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
      {
        DebugSettings settings = SettingsFile.Default.GetSettings<DebugSettings>();

        if (settings != null)
        {
          ExecutionControl.BreakpointAddress = settings.BreakpointAddress;

          UpdateBreakpointAddress();
        }
      }
    }

    private void UpdateBreakpointAddress()
    {
      int address = ExecutionControl.BreakpointAddress;

      if (address != -1)
        tbAddress.Text = address.ToString("X4");
      else
        tbAddress.Text = "-";
    }
  }
}
