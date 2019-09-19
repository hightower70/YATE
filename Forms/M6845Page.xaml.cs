﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using TVCEmu.Controls;
using TVCHardware;

namespace TVCEmu.Forms
{
	/// <summary>
	/// Interaction logic for VideoPage.xaml
	/// </summary>
	public partial class M6845Page : UserControl, INotifyPropertyChanged
	{
		public static DependencyProperty ExecutionControlProperty = DependencyProperty.Register("ExecutionControl", typeof(ExecutionControl), typeof(M6845Page), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnExecutionControlPropertyChanged)));

		private static void OnExecutionControlPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((M6845Page)sender).RegisterDebugEventHandler((ExecutionControl)e.NewValue);
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
			// get register values from the video device
			for (int i = 0; i < TVCVideo.MC6845RegisterCount; i++)
			{
				Registers[i] = m_execution_control.TVC.Video.M6845Registers[i];
			}

			NotifyPropertyChanged("RegisterAddress");
		}

		public ObservableCollection<byte> Registers { get; private set; }
		public byte RegisterAddress
		{
			get { return m_execution_control.TVC.Video.M6845RegisterAddress; }
			set { m_execution_control.TVC.Video.M6845RegisterAddress = value; }
		}

		public M6845Page()
		{
			InitializeComponent();
			Registers = new ObservableCollection<byte>();

			for (int i = 0; i < TVCVideo.MC6845RegisterCount; i++)
			{
				Registers.Add(0);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}
