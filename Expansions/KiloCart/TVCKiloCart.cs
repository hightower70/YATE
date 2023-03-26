///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2013-2023 Laszlo Arvai. All rights reserved.
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
// 64kB ROM cartridge main emulation class
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Runtime.CompilerServices;
using YATECommon;

namespace KiloCart
{
  public class TVCKiloCart : ITVCCartridge
  { 
    private const UInt16 PAGE0_ADDRESS = 0x3ffc;

    private KiloCartSettings m_settings;
    private KilocartMemory m_memory;

    /// <summary>
    /// Activtes settings information
    /// </summary>
    /// <param name="in_settings">Cart settings</param>
    /// <returns></returns>
    public bool SetSettings(KiloCartSettings in_settings)
    {
      bool restart_tvc = false;
      m_settings = in_settings;

      // load ROM content
      m_memory = new KilocartMemory();
      if (m_memory.Initialize(m_settings.ROMFileName))
        restart_tvc = true;

      return restart_tvc;
    }

    /// <summary>
    /// CPU reads memory content
    /// </summary>
    /// <param name="in_address">Address of the memory</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte MemoryRead(ushort in_address)
    {
      // check if page register selected
      if (in_address >= PAGE0_ADDRESS)
      {
        m_memory.SetPageRegister((byte)(in_address - PAGE0_ADDRESS));
      }

      // return memory content
      return m_memory.MemoryRead(in_address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // not supported
    }

    public void Reset()
    {
      // no RESET available on CART
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
      // non relevant
    }
  }
}
