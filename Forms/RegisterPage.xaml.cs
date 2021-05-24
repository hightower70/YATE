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
  public partial class RegisterPage : UserControl, INotifyPropertyChanged
	{
		public static DependencyProperty ExecutionControlProperty = DependencyProperty.Register("ExecutionControl", typeof(ExecutionManager), typeof(RegisterPage), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnExecutionControlPropertyChanged)));

		private static void OnExecutionControlPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((RegisterPage)sender).RegisterDebugEventHandler((ExecutionManager)e.NewValue);
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
			NotifyPropertyChanged("AF");
			NotifyPropertyChanged("BC");
			NotifyPropertyChanged("DE");
			NotifyPropertyChanged("HL");
			NotifyPropertyChanged("IX");
			NotifyPropertyChanged("IY");
			NotifyPropertyChanged("PC");
			NotifyPropertyChanged("SP");
			NotifyPropertyChanged("IR");
			NotifyPropertyChanged("WZ");

      NotifyPropertyChanged("AF_");
      NotifyPropertyChanged("BC_");
      NotifyPropertyChanged("DE_");
      NotifyPropertyChanged("HL_");
    }

    public RegisterPage()
		{
			InitializeComponent();
		}

		public ushort AF
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.AF;
			}
		}

		public ushort BC
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.BC;
			}
		}

		public ushort DE
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.DE;
			}
		}

		public ushort HL
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.HL;
			}
		}

		public ushort AF_
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers._AF_;
			}
		}

		public ushort BC_
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers._BC_;
			}
		}

		public ushort DE_
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers._DE_;
			}
		}

		public ushort HL_
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers._HL_;
			}
		}

		public ushort IX
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.IX;
			}
		}

		public ushort IY
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.IY;
			}
		}

		public ushort PC
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.PC;
			}
		}

		public ushort SP
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.SP;
			}
		}

		public ushort IR
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.IR;
			}
		}

		public ushort WZ
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.WZ;
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
