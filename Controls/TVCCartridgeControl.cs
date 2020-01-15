using System;
using System.IO;
using TVCEmu.Forms;
using TVCEmu.Models.TVCHardware;
using TVCHardware;

namespace TVCEmu.Controls
{
	public class TVCCartridgeControl
	{
		private ExecutionControl m_execution_control;

		public void Initialize(MainWindow in_main_window, ExecutionControl in_execution_control)
		{
			m_execution_control = in_execution_control;
		}

		public void OnCartridgeMemoryLoad()
		{
			// Configure open file dialog box
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
			{
				DefaultExt = ".crt",
				Filter = "Cartridge file (*.crt)|*.CRT|Binary file (*.bin)|*.bin|ROM file (*.rom)|*.rom"
			};

			// Show open file dialog box
			bool? result = null;
			result = dlg.ShowDialog();

			// Process open file dialog box results
			if (result == true)
			{
				string file_name = dlg.FileName;

				// load cart content
				FileInfo file = new FileInfo(file_name);
				long file_length = file.Length;

				if (file_length > TVCCartridge.CartMemLength)
					file_length = TVCCartridge.CartMemLength;

				byte[] file_data;

				using (FileStream fs = File.OpenRead(file_name))
				{
					using (BinaryReader binaryReader = new BinaryReader(fs))
					{
						file_data = binaryReader.ReadBytes((int)fs.Length);
					}
				}

				// stop execution
				m_execution_control.ChangeExecutionState(ExecutionControl.ExecutionStateRequest.Pause);

				// copy content to the memory
				byte[] cart_memory = ((TVCCartridge)m_execution_control.TVC.Cartridge).Memory;
				Array.Copy(file_data, 0, cart_memory, 0, file_length);

				// clear remaining memory
				for (int i = (int)file_length; i < TVCCartridge.CartMemLength; i++)
				{
					cart_memory[i] = 0xff;
				}

				// reset computer
				m_execution_control.TVC.ColdReset();

				// restore execution state
				m_execution_control.ChangeExecutionState(ExecutionControl.ExecutionStateRequest.Restore);
			}
		}

		public void OnCartridgeMemoryClear()
		{
			// stop execution
			m_execution_control.ChangeExecutionState(ExecutionControl.ExecutionStateRequest.Pause);

			// clear remaining memory
			byte[] cart_memory = ((TVCCartridge)m_execution_control.TVC.Cartridge).Memory;
			for (int i = 0; i < TVCCartridge.CartMemLength; i++)
			{
				cart_memory[i] = 0xff;
			}

			// reset computer
			m_execution_control.TVC.ColdReset();

			// restore execution state
			m_execution_control.ChangeExecutionState(ExecutionControl.ExecutionStateRequest.Restore);
		}

	}
}
