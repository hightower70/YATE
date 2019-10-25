using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVCEmu.Controls
{
	public class ExecutionHistoryCollection
	{
		private ExecutionHistoryEntry[] m_write_history;
		private ExecutionHistoryEntry[] m_read_history;
		private int m_write_index;
		private int m_read_index;

		public ExecutionHistoryCollection(int in_element_count)
		{
			m_read_history = new ExecutionHistoryEntry[in_element_count];
			m_write_history = new ExecutionHistoryEntry[in_element_count];

			for (int i = 0; i < m_write_history.Length; i++)
			{
				m_read_history[i] = new ExecutionHistoryEntry();
				m_write_history[i] = new ExecutionHistoryEntry();
			}
		}

		public ExecutionHistoryEntry GetNextEmptySlot()
		{
			m_write_index--;

			if (m_write_index < 0)
				m_write_index = m_write_history.Length - 1;

			return m_write_history[m_write_index];
		}

		public void UpdateHistory()
		{
			m_read_index = m_write_index;
			for (int i = 0; i < m_write_history.Length; i++)
				m_write_history[i].CopyTo(m_read_history[i]);
		}


		public ExecutionHistoryEntry this[int in_index]
		{
			get
			{
				int index = (m_read_index + in_index) % m_write_history.Length;

				return m_write_history[index];
			}
		}

	}

}
