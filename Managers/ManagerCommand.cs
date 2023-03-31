///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2020 Laszlo Arvai. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
// MA 02110-1301  USA
///////////////////////////////////////////////////////////////////////////////
// File description
// ----------------
// Command class for various manager classes for UI binding
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Windows.Input;

namespace YATE.Managers
{
	public class ManagerCommand : ICommand
	{
		public delegate void ICommandOnExecute(object parameter);


		private ICommandOnExecute m_execute;
		private bool m_can_execute;

		public ManagerCommand(ICommandOnExecute onExecuteMethod)
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
