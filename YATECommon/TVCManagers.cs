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
// Videoton TV Computer Managers Classes Collection
///////////////////////////////////////////////////////////////////////////////
using System.Windows;
using YATECommon.Expansions;

namespace YATECommon
{
  public class TVCManagers
  {
    public Window MainWindow { get; private set; }
    public IAudioManager AudioManager { get; private set; }
    public ExpansionManager ExpansionManager { get; private set; }
    public IExecutionManager ExecutionManager { get; private set; }
    public ICartridgeManager CartridgeManager { get; private set; }
    public IDebugManager DebugManager { get; private set; }
    public IBreakpointManager BreakpointManager { get; private set; }

    public IPrinterManager PrinterManager { get; private set; }

    public void SetMainWindow(Window in_window)
    {
      MainWindow = in_window;
    }

    public void SetAudioManager(IAudioManager in_audio_manager)
    {
      AudioManager = in_audio_manager;
    }

    public void SetExecutionManager(IExecutionManager in_execution_manager)
    {
      ExecutionManager = in_execution_manager;
    }

    public void SetExpansionManager(ExpansionManager in_expansion_manager)
    {
      ExpansionManager = in_expansion_manager;
    }

    public void SetCartridgeManager(ICartridgeManager in_cartridge_manager)
    {
      CartridgeManager = in_cartridge_manager;
    }

    public void SetDebugManager(IDebugManager in_debug_manager)
    {
      DebugManager = in_debug_manager;
    }

    public void SetBreakpointManager(IBreakpointManager in_breakpoint_manager)
    {
      BreakpointManager = in_breakpoint_manager;
    }

    public void SetPrinterManager(IPrinterManager in_printer_manager)
    {
      PrinterManager = in_printer_manager;
    }

    #region · Singleton members ·

    private static TVCManagers m_default;

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static TVCManagers Default
    {
      get
      {
        if (m_default == null)
        {
          m_default = new TVCManagers();
        }

        return m_default;
      }
    }
    #endregion
  }
}
