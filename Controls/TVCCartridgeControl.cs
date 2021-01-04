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
// Videoton TV Computer ROM Cartridge UI Control
///////////////////////////////////////////////////////////////////////////////
using System.IO;
using YATE.Emulator.TVCHardware;
using YATE.Forms;
using YATE.Settings;
using YATECommon.Settings;

namespace YATE.Controls
{
  /// <summary>
  /// TVC Cartridge UI control class
  /// </summary>
	public class TVCCartridgeControl
	{
		private ExecutionControl m_execution_control;
    private MainWindow m_main_window;

		public void Initialize(MainWindow in_main_window, ExecutionControl in_execution_control)
		{
			m_execution_control = in_execution_control;
      m_main_window = in_main_window;
		}

    /// <summary>
    /// Handle cartridge memory load UI event
    /// </summary>
		public void OnCartridgeMemoryLoad()
		{
      // only supported for the standard cartridge
      if (!(m_execution_control.TVC.Cartridge is TVCCartridge))
        return;

      // Configure open file dialog box
      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
      {
        DefaultExt = ".crt",
        Filter = "Cartridge file (*.crt)|*.CRT|Binary file (*.bin)|*.bin|ROM file (*.rom)|*.rom"
			};

      // Get cartridge file folder
      TVCConfigurationSettings settings = SettingsFile.Default.GetSettings<TVCConfigurationSettings>();

      if (settings != null && !string.IsNullOrEmpty(settings.CartridgeFileName))
      {
        dlg.InitialDirectory = Path.GetDirectoryName(settings.CartridgeFileName);
      }

      // Show open file dialog box
      bool? result = null;
			result = dlg.ShowDialog();

			// Process open file dialog box results
			if (result == true)
			{
				string file_name = dlg.FileName;

        // stop execution
        m_execution_control.ChangeExecutionState(ExecutionControl.ExecutionStateRequest.Pause);

        try
        {
          TVCCartridge cartridge = (TVCCartridge)m_execution_control.TVC.Cartridge;
          cartridge.ReadCartridgeFile(file_name);
        }
        catch
        {
          CustomControls.CustomMessageBox msgbox = new CustomControls.CustomMessageBox(m_main_window);
          msgbox.ShowMessageBoxFromResource("srError", "srFileLoadError", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

				// reset computer
				m_execution_control.TVC.ColdReset();

				// restore execution state
				m_execution_control.ChangeExecutionState(ExecutionControl.ExecutionStateRequest.Restore);

        // save cartidge file name
        settings.CartridgeActive = true;
        settings.CartridgeFileName = file_name;
        SettingsFile.Default.SetSettings(settings);
        SettingsFile.Default.Save();
      }
    }

    /// <summary>
    /// Handles cartridge memory clean
    /// </summary>
		public void OnCartridgeMemoryClear()
		{
      // only supported for the standard cartridge
      if (!(m_execution_control.TVC.Cartridge is TVCCartridge))
        return;

      // stop execution
      m_execution_control.ChangeExecutionState(ExecutionControl.ExecutionStateRequest.Pause);

			// clear remaining memory
			((TVCCartridge)m_execution_control.TVC.Cartridge).ClearCartridge();

			// reset computer
			m_execution_control.TVC.ColdReset();

			// restore execution state
			m_execution_control.ChangeExecutionState(ExecutionControl.ExecutionStateRequest.Restore);

      // save cartidge file name
      TVCConfigurationSettings settings = SettingsFile.Default.GetSettings<TVCConfigurationSettings>();
      settings.CartridgeActive = false;
      SettingsFile.Default.SetSettings(settings);
      SettingsFile.Default.Save();
    }
  }
}
