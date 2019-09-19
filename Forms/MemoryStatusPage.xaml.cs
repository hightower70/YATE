﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using TVCEmu.Controls;
using TVCHardware;

namespace TVCEmu.Forms
{
	/// <summary>
	/// Interaction logic for MemoryMappingView.xaml
	/// </summary>
	public partial class MemoryStatusPage : UserControl, INotifyPropertyChanged
	{
		public static DependencyProperty ExecutionControlProperty = DependencyProperty.Register("ExecutionControl", typeof(ExecutionControl), typeof(MemoryStatusPage), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnExecutionControlPropertyChanged)));

		private static void OnExecutionControlPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((MemoryStatusPage)sender).RegisterDebugEventHandler((ExecutionControl)e.NewValue);
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
			NotifyPropertyChanged("Port02H");
			NotifyPropertyChanged("Page0MappedName");
			NotifyPropertyChanged("Page1MappedName");
			NotifyPropertyChanged("Page2MappedName");
			NotifyPropertyChanged("Page3MappedName");
		}

		public MemoryStatusPage()
		{
			InitializeComponent();
		}


		public string Port02H
		{
			get
			{
				return m_execution_control.TVC.Memory.Port02H.ToString("X2");
			}
			set
			{
			}
		}

		public string Page0MappedName
		{
			get { return m_execution_control.TVC.Memory.GetPageMappingNameString(0); }
		}

		public string Page1MappedName
		{
			get { return m_execution_control.TVC.Memory.GetPageMappingNameString(1); }
		}

		public string Page2MappedName
		{
			get { return m_execution_control.TVC.Memory.GetPageMappingNameString(2); }
		}

		public string Page3MappedName
		{
			get { return m_execution_control.TVC.Memory.GetPageMappingNameString(3); }
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
