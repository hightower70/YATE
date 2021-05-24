using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using YATE.Managers;
using YATE.Emulator.TVCHardware;

namespace YATE.Forms
{
  /// <summary>
  /// Interaction logic for RegisterPage.xaml
  /// </summary>
  public partial class FlagsPage : UserControl, INotifyPropertyChanged
	{
		public static DependencyProperty ExecutionControlProperty = DependencyProperty.Register("ExecutionControl", typeof(ExecutionManager), typeof(FlagsPage), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnExecutionControlPropertyChanged)));

		private static void OnExecutionControlPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((FlagsPage)sender).RegisterDebugEventHandler((ExecutionManager)e.NewValue);
		}

		public ExecutionManager ExecutionControl
		{
			get { return (ExecutionManager)GetValue(ExecutionControlProperty); }
			set { SetValue(ExecutionControlProperty, value); }
		}

		private void RegisterDebugEventHandler(ExecutionManager in_execution_control)
		{
			in_execution_control.DebuggerBreakEvent += DebuggerBreakEventDelegate;
			m_execution_control = in_execution_control;
			DataContext = this;
		}

		private ExecutionManager m_execution_control;

		private void DebuggerBreakEventDelegate(TVComputer in_sender)
		{
			NotifyPropertyChanged("FS");
			NotifyPropertyChanged("FZ");
			NotifyPropertyChanged("F5");
			NotifyPropertyChanged("FH");
			NotifyPropertyChanged("F3");
			NotifyPropertyChanged("FPV");
			NotifyPropertyChanged("FN");
			NotifyPropertyChanged("FC");
    }

    public FlagsPage()
		{
			InitializeComponent();
		}

		public string FS
		{
			get
			{
        return m_execution_control.TVC.CPU.Registers.SFlag ? "1" : "0";
			}
		}

		public string FZ
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.ZFlag ? "1" : "0";
      }
		}

		public string F5
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.R5Flag ? "1" : "0";
			}
		}

		public string FH
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.HFlag ? "1" : "0";
			}
		}

		public string F3
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.R3Flag ? "1" : "0"; 
			}
		}

		public string FPV
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.PFlag ? "1" : "0";
			}
		}

		public string FN
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.NFlag ? "1" : "0";
      }
		}

		public string FC
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.CFlag ? "1" : "0";
      }
		}

		#region · INotifyPropertyChangedEvent ·

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
