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
// SD Card handler Cartride man emulation class
///////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using YATECommon;

namespace SDCart
{
  public class SDCart : ITVCCartridge
  {
    public const int MaxCartRomSize = 1024 * 1024;

    public byte[] Rom { get; private set; }

    public SDCartSettings Settings { get; private set; }

    private ushort m_map_register = 0;
    private CoProcessor m_coproc;

    public void SetSettings(SDCartSettings in_settings)
    {
      Settings = in_settings;

      m_coproc = new CoProcessor(this);

      // load ROM
      Rom = new byte[MaxCartRomSize];
      for (int i = 0; i < Rom.Length; i++)
        Rom[i] = 0xff;

      LoadROMContent(@"d:\Projects\Retro\SDCART\CartProgram\sdcart.bin", 0);

      // initialize members
      m_map_register = 0;
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
      ushort address = (ushort)(in_address & ~0xc000);

      if((address & 0x2000) == 0)
      {
        // ROM access
        return Rom[address | m_map_register];
      }
      else
      {
        int range = (address >> 11) & 3;

        switch(range)
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
