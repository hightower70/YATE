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
// 1MByte ROM cartridge main emulation class
///////////////////////////////////////////////////////////////////////////////
using System;
using YATECommon;
using YATECommon.Helpers;

namespace MegaCart
{
  public class TVCMegaCart : ITVCCartridge, IDebuggableMemory
  {
    #region · Constants ·
    public const int RomPageCount = 64;
    public const int RomPageSize = 16384;
    public const int MaxCartRomSize = 1024 * 1024;
    private const UInt16 PAGE_REGISTER_ADDRESS_MASK = 0x3fff;
    private const UInt16 PAGE_REGISTER_MIN_ADDRESS = 0x3c00;
    private const UInt16 PAGE_REGISTER_MAX_ADDRESS = 0x3fff;
    private const byte PAGE_REGISTER_MASK = 0x3f;
    #endregion

    #region · Data members ·
    private MegaCartSettings m_settings;
    public byte[] Rom { get; private set; }
    private byte m_page_register = 0;

    private readonly string[] m_rom_page_names;
    #endregion

    #region · Constructor · 
    public TVCMegaCart()
    {
      Rom = new byte[MaxCartRomSize];
      ROMFile.FillMemory(Rom);
      m_page_register = 0;

      // create page names
      string[] rom_page_names = new string[RomPageCount];
      for (int i = 0; i < rom_page_names.Length; i++)
      {
        rom_page_names[i] = String.Format("ROM[{0}]", i);
      }
      m_rom_page_names = rom_page_names;
    }
    #endregion

    #region · Public methods ·

    public bool SetSettings(MegaCartSettings in_settings)
    {
      m_settings = in_settings;

       // initialize members
      m_page_register = 0;

      bool settings_changed = false;

      // load ROM content
      ROMFile.LoadMemoryFromFile(m_settings.ROMFileName, Rom, ref settings_changed);

      return settings_changed;
    }

    #endregion

    #region · ITVCCartridge implementation ·

    /// <summary>
    /// Reads cartridge memory
    /// </summary>
    /// <param name="in_address"></param>
    /// <returns></returns>
    public byte MemoryRead(ushort in_address)
    {
      byte retval = Rom[in_address + (m_page_register << 14)];

      // emulate hw bug
      //if ((in_address & PAGE_REGISTER_ADDRESS_MASK) >= PAGE_REGISTER_MIN_ADDRESS)
      //  m_page_register = (byte)(retval & PAGE_REGISTER_MASK); 

      return retval;
    }

    /// <summary>
    /// Writes cartridge memory
    /// </summary>
    /// <param name="in_address"></param>
    /// <param name="in_byte"></param>
    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      if((in_address & PAGE_REGISTER_ADDRESS_MASK) >= PAGE_REGISTER_MIN_ADDRESS)
      {
        m_page_register = (byte)(in_byte & PAGE_REGISTER_MASK);
      }
    }

    /// <summary>
    /// Resets computer
    /// </summary>
    public void Reset()
    {
      
    }

    // cartridge handling functions

    /// <summary>
    /// Inserts cardtridge (installation)
    /// </summary>
    /// <param name="in_parent"></param>
    public void Insert(ITVComputer in_parent)
    {
      TVCManagers.Default.DebugManager.RegisterDebuggableMemory(this);
    }

    /// <summary>
    /// Removes the cartridges (deinstallation)
    /// </summary>
    /// <param name="in_parent"></param>
    public void Remove(ITVComputer in_parent)
    {
      TVCManagers.Default.DebugManager.UnregisterDebuggableMemory(this);
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
      // non relevant
    }
    #endregion

    #region · IDebuggableInterface implementation ·

    public TVCMemoryType MemoryType { get { return TVCMemoryType.Cart; } }

    public int AddressOffset { get { return 0xc000; } }

    public int MemorySize { get { return MaxCartRomSize; } }

    public int PageCount { get { return RomPageCount; } }

    public int PageIndex { get { return m_page_register & PAGE_REGISTER_MASK; } }

    public string[] PageNames { get { return m_rom_page_names; } }

    public byte DebugReadMemory(int in_page_index, int in_address)
    {
      return Rom[in_address + in_page_index * RomPageSize];
    }

    public void DebugWriteMemory(int in_page_index, int in_address, byte in_data)
    {
      Rom[in_address + in_page_index * RomPageSize] = in_data;
    }

    #endregion
  }
}
