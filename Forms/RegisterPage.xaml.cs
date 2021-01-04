using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using YATE.Controls;
using YATE.Emulator.TVCHardware;

namespace YATE.Forms
{
  /// <summary>
  /// Interaction logic for RegisterPage.xaml
  /// </summary>
  public partial class RegisterPage : UserControl, INotifyPropertyChanged
	{
		public static DependencyProperty ExecutionControlProperty = DependencyProperty.Register("ExecutionControl", typeof(ExecutionControl), typeof(RegisterPage), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnExecutionControlPropertyChanged)));

		private static void OnExecutionControlPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((RegisterPage)sender).RegisterDebugEventHandler((ExecutionControl)e.NewValue);
		}

		public ExecutionControl ExecutionControl
		{
			get { return (ExecutionControl)GetValue(ExecutionControlProperty); }
			set { SetValue(ExecutionControlProperty, value); }
		}

		private void RegisterDebugEventHandler(ExecutionControl in_execution_control)
		{
			in_execution_control.DebuggerBreakEvent += DebuggerBreakEventDelegate;
			m_execution_control = in_execution_control;
			DataContext = this;
		}

		private ExecutionControl m_execution_control;

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
				return m_execution_control.TVC.CPU.Registers.AF;
			}
		}

		public ushort BC_
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.BC;
			}
		}

		public ushort DE_
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.DE;
			}
		}

		public ushort HL_
		{
			get
			{
				return m_execution_control.TVC.CPU.Registers.HL;
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
