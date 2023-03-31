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
// SD Card handler Cartride main emulation class
///////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using YATECommon;
using YATECommon.Helpers;

namespace SDCart
{
  public class SDCart : ITVCCartridge, IDebuggableMemory
  {
    #region · Constants ·
    public const int RomPageCount = 4;
    public const int RomPageSize = 16384;
    public const int RomSize = RomPageCount * RomPageSize;
    #endregion

    #region · Data members ·
    public byte[] Rom { get; private set; }

    public SDCartSettings Settings { get; private set; }

    private ushort m_page_register = 0;
    private int m_page_index = 0;
    private int m_page_address = 0;
    private CoProcessor m_coproc;

    private readonly string[] m_rom_page_names;

    #endregion

    #region · Constructor ·

    /// <summary>
    /// Constructor
    /// </summary>
    public SDCart()
    {
      Rom = new byte[RomSize];
      ROMFile.FillMemory(Rom);

      string[] rom_page_names = new string[RomPageCount];
      for (int i = 0; i < rom_page_names.Length; i++)
      {
        rom_page_names[i] = String.Format("ROM[{0}]", i);
      }
      m_rom_page_names = rom_page_names;
    }

    #endregion

    public bool SetSettings(SDCartSettings in_settings)
    {
      bool restart_required = false;
      Settings = in_settings;

      m_coproc = new CoProcessor(this);

      // load ROM
      ROMFile.LoadMemoryFromResource("SDCart.Resources.sdcart.bin", Rom, ref restart_required);

      // initialize members
      m_page_register = 0;

      return restart_required;
    }

    public void SetPageRegister(byte in_register)
    {
      m_page_register = in_register;
      m_page_index = in_register;
      m_page_address = RomPageSize * m_page_index;
    }

    public byte MemoryRead(ushort in_address)
    {
      ushort address = (ushort)(in_address & ~0xc000);

      if ((address & 0x2000) == 0)
      {
        // ROM access
        return Rom[address + m_page_address];
      }
      else
      {
        int range = (address >> 11) & 3;

        switch (range)
        {
          case 0:
            return m_coproc.CoProcRead();

          case 1:
            m_coproc.CoProcWrite((byte)(address & 0xff));
            return 0xff;

          case 2:
            return 0xff;

          case 3:
            m_coproc.CoProcInt();
            return 0xff;

          default:
            return 0xff;
        }
      }
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // not supported
    }

    public void Reset()
    {
      // non relevant
    }

    public void Insert(ITVComputer in_parent)
    {
      TVCManagers.Default.DebugManager.RegisterDebuggableMemory(this);
    }

    public void Remove(ITVComputer in_parent)
    {
      TVCManagers.Default.DebugManager.UnregisterDebuggableMemory(this);
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
      // non relevant
    }

    #region · IDebuggableMemory implementation ·
    public TVCMemoryType MemoryType { get { return TVCMemoryType.Cart; } }

    public int AddressOffset { get { return 0xc000; } }

    public int MemorySize { get { return RomPageSize; } }

    public int PageCount { get { return RomPageCount; } }

    public int PageIndex { get { return m_page_index; } }

    public string[] PageNames { get { return m_rom_page_names; } }
    public byte DebugReadMemory(int in_page_index, int in_address)
    {
      return Rom[in_page_index * RomPageSize + in_address];
    }

    public void DebugWriteMemory(int in_page_index, int in_address, byte in_data)
    {
      Rom[in_page_index * RomPageSize + in_address] = in_data;
    }

    #endregion
  }
}
