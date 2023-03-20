///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2023 Laszlo Arvai. All rights reserved.
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
// MultiCart main emulation class
///////////////////////////////////////////////////////////////////////////////
using MultiCart;
using System;
using System.Runtime.CompilerServices;
using YATECommon;

namespace Multicart
{
  public class TVCMultiCart : ITVCCartridge
  {
    #region · Constants ·
    public const int ROMSaveTimeout = 3000; // ROM Save timeout in ms (after this time the content of the ROM is saved)
    #endregion

    #region · Data members ·

    private MultiCartMemory m_memory;
    private MultiCartSettings m_settings;

    #endregion

    public TVCMultiCart()
    {
      m_memory = new MultiCartMemory();
    }

    public bool SetSettings(MultiCartSettings in_settings)
    {
      bool restart_tvc = false;

      if (m_settings != null && m_settings.RAMSize != in_settings.RAMSize)
        restart_tvc = true;

      m_settings = in_settings;

      // load ROM
      if (m_memory.Initialize(m_settings.ROM1FileName, m_settings.ROM2FileName, (int)(64 * 1024 * Math.Pow(2, m_settings.RAMSize))))
        restart_tvc = true;

      return restart_tvc;
    }


    public byte MemoryRead(ushort in_address)
    {
      return m_memory.MemoryRead(in_address);
    }


    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      m_memory.MemoryWrite(in_address, in_byte);
    }

    public void Reset()
    {
      if (!m_memory.IsRegisterLocked)
        m_memory.SetRegister(0);
    }

    public void Initialize(ITVComputer in_parent)
    {
      TVCManagers.Default.DebugManager.RegisterDebuggableMemory(m_memory);
    }

    public void Remove(ITVComputer in_parent)
    {
      TVCManagers.Default.DebugManager.UnregisterDebuggableMemory(m_memory);
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
      // save ROM content if enabled
      if (m_settings.AutosaveFlashContent && m_memory.IsROMContentChanged && (DateTime.Now - m_memory.ROMContentChangedTimestamp).TotalMilliseconds > ROMSaveTimeout)
      {
        m_memory.SaveROMContent();
      }
    }
  }
}
