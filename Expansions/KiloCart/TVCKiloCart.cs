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
using System.IO;
using YATECommon;

namespace KiloCart
{
  public class TVCKiloCart : ITVCCartridge
  {
    private KiloCartSettings m_settings;

    public const int MaxCartRomSize = 64 * 1024;
    private const UInt16 PAGE0_ADDRESS = 0x3ffc;

    public byte[] Rom { get; private set; }

    private byte m_page_register = 0;

    public void SetSettings(KiloCartSettings in_settings)
    {
      m_settings = in_settings;

      // load ROM
      Rom = new byte[MaxCartRomSize];
      for (int i = 0; i < Rom.Length; i++)
        Rom[i] = 0xff;

      LoadROMContent(m_settings.ROMFileName, 0);

      // initialize members
      m_page_register = 3;
    }

    private void LoadROMContent(string in_rom_file_name, int in_address)
    {
      if (!string.IsNullOrEmpty(in_rom_file_name))
      {
        byte[] data = File.ReadAllBytes(in_rom_file_name);

        int length = data.Length;
                   
        if ((in_address + data.Length) > Rom.Length)
          length = Rom.Length - in_address;

        Array.Copy(data, 0, Rom, in_address, length);
      }
    }

    public byte MemoryRead(ushort in_address)
    {
      if(in_address >= PAGE0_ADDRESS)
      {
        m_page_register = (byte)(in_address - PAGE0_ADDRESS);
      }

      return Rom[in_address + (m_page_register << 14)];
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // not supported
    }

    public void Reset()
    {
      m_page_register = 0;
    }

    public void Initialize(ITVComputer in_parent)
    {
      // non relevant
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
