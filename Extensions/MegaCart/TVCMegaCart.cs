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
  public class TVCMegaCart : ITVCCartridge
  {
    private MegaCartSettings m_settings;

    public const int MaxCartRomSize = 1024 * 1024;
    private const UInt16 PAGE_REGISTER_ADDRESS_MASK = 0x3fff;
    private const UInt16 PAGE_REGISTER_MIN_ADDRESS = 0x3c00;
    private const UInt16 PAGE_REGISTER_MAX_ADDRESS = 0x3fff;
    private const byte PAGE_REGISTER_MASK = 0x3f;

    public byte[] Rom { get; private set; }

    private byte m_page_register = 0;

    public bool SetSettings(MegaCartSettings in_settings)
    {
      m_settings = in_settings;

       // initialize members
      m_page_register = 0;

      bool settings_changed = LoadROMContent(m_settings.ROMFileName, 0);

      return settings_changed;
    }

    private bool LoadROMContent(string in_rom_file_name, int in_address)
    {
      bool changed = false;

      // save old rom
      byte[] old_rom = Rom;

      // load ROM
      Rom = new byte[MaxCartRomSize];
      for (int i = 0; i < Rom.Length; i++)
        Rom[i] = 0xff;

      ROMFile.LoadMemoryFromFile(in_rom_file_name, Rom);

      changed = !ROMFile.IsMemoryEqual(old_rom, Rom);

      return changed;
    }

    public byte MemoryRead(ushort in_address)
    {
      byte retval = Rom[in_address + (m_page_register << 14)];

      // emulate hw bug
      //if ((in_address & PAGE_REGISTER_ADDRESS_MASK) >= PAGE_REGISTER_MIN_ADDRESS)
      //  m_page_register = (byte)(retval & PAGE_REGISTER_MASK); 

      return retval;
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      if((in_address & PAGE_REGISTER_ADDRESS_MASK) >= PAGE_REGISTER_MIN_ADDRESS)
      {
        m_page_register = (byte)(in_byte & PAGE_REGISTER_MASK);
      }
    }

    public void Reset()
    {
      m_page_register = 0;
    }

    public void Initialize(ITVComputer in_parent)
    {
      //non relevant
    }

    public void Remove(ITVComputer in_parent)
    {
      // non relevant
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
      // non relevant
    }
  }
}
