using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TVCEmu.Controls
{
	public class ExecutionControlCommand : ICommand
	{
		public delegate void ICommandOnExecute(object parameter);


		private ICommandOnExecute m_execute;
		private bool m_can_execute;

		public ExecutionControlCommand(ICommandOnExecute onExecuteMethod)
		{
			m_execute = onExecuteMethod;
			m_can_execute = true;
		}

		#region ICommand Members

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			return m_can_execute;
		}

		public void SetCanExecute(bool in_can_execute)
		{
			if (m_can_execute != in_can_execute)
			{
				m_can_execute = in_can_execute;
				CanExecuteChanged.Invoke(this, EventArgs.Empty);
			}
		}

		public void Execute(object parameter)
		{
			m_execute.Invoke(parameter);
		}

		#endregion
	}
}
