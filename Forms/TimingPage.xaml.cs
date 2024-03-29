﻿using System;
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
  public partial class TimingPage : UserControl, INotifyPropertyChanged
	{
		private DateTime m_prev_timestamp;
		private ulong m_prev_t_state;

		private float m_cpu_clock;
		//private float m_video_refresh_rate;


		public static DependencyProperty ExecutionControlProperty = DependencyProperty.Register("ExecutionControl", typeof(ExecutionManager), typeof(TimingPage), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnExecutionControlPropertyChanged)));

		private static void OnExecutionControlPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((TimingPage)sender).RegisterDebugEventHandler((ExecutionManager)e.NewValue);
		}

		public ExecutionManager ExecutionControl
		{
			get { return (ExecutionManager)GetValue(ExecutionControlProperty); }
			set { SetValue(ExecutionControlProperty, value); }
		}

		private void RegisterDebugEventHandler(ExecutionManager in_execution_control)
		{
			in_execution_control.DebuggerAnimationEvent += DebuggerBreakEventDelegate;
			m_execution_control = in_execution_control;
			DataContext = this;
		}

		private ExecutionManager m_execution_control;

		private void DebuggerBreakEventDelegate(TVComputer in_sender)
		{
			NotifyPropertyChanged("TState");

			DateTime current_timestamp = DateTime.Now;
			double ellapsed_time_in_ms = (current_timestamp - m_prev_timestamp).TotalMilliseconds;
			if (ellapsed_time_in_ms > 1000)
			{
				ulong t_state = m_execution_control.TVC.CPU.TotalTState;

				CPUClock = (float)((t_state - m_prev_t_state) / 1000000000.0 * ellapsed_time_in_ms);

				m_prev_timestamp = current_timestamp;
				m_prev_t_state = t_state;
			}
		}

		public TimingPage()
		{
			InitializeComponent();
			m_prev_timestamp = DateTime.Now;
		}

		public float TState
		{
			get
			{
				return m_execution_control.TVC.CPU.TotalTState;
			}

		}

		public float CPUClock
		{
			get
			{
				return m_cpu_clock;
			}
			internal set
			{
				m_cpu_clock = value;

				NotifyPropertyChanged();
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
