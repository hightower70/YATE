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
using YATECommon;
using YATECommon.Chips;

namespace Multicart
{
  public class TVCMultiCart : ITVCCartridge
  {
    #region · Constants ·
    public const int MaxCartRomSize = 1024 * 1024;
    public const int MaxCartRamSize = 512 * 1024;
    public const int RamAddressMask = MaxCartRamSize - 1;
    public const int ROMSaveTimeout = 3000; // ROM Save timeout in ms (after this time the content of the ROM is saved)
    #endregion

    #region · Data members ·

    private MultiCartSettings m_settings;

    private SST39SF040 m_high_rom;
    private SST39SF040 m_low_rom;
    public byte[] m_ram;

    private byte m_register = 0;
    private int m_page_start_address = 0;
    private byte m_rom_select;
    private int m_register_start_address = 0;
    private bool m_ram_select = false;
    private bool m_register_locked = false;
    private int m_ram_size;

    private bool m_rom_content_changed;
    private DateTime m_rom_changed_timestamp;


    #endregion

    public TVCMultiCart()
    {
      m_high_rom = new SST39SF040();
      m_low_rom = new SST39SF040();

      m_register = 0;
      m_page_start_address = 0;
      m_rom_select = 0;
      m_register_start_address = 0;
      m_ram_select = false;
      m_register_locked = false;
      m_rom_content_changed = false;
    }

    public bool SetSettings(MultiCartSettings in_settings)
    {
      bool restart_tvc = false;

      if (m_settings != null && m_settings.RAMSize != in_settings.RAMSize)
        restart_tvc = true;

      m_settings = in_settings;

      // load ROM
      m_low_rom.ROMFileName = m_settings.ROM2FileName;
      m_low_rom.Load();
      m_high_rom.ROMFileName = m_settings.ROM1FileName;
      m_high_rom.Load();

      // init RAM
      m_ram = new byte[MaxCartRamSize];

      // initialize members
      m_register = 0;
      m_page_start_address = 0;
      m_register_start_address = 0;
      m_ram_select = false;
      m_register_locked = false;
      
      m_ram_size = (int)(64 * 1024 * Math.Pow(2, m_settings.RAMSize));

      return restart_tvc;
    }

    public byte MemoryRead(ushort in_address)
    {
      if (!m_register_locked && in_address >= m_register_start_address && in_address < m_register_start_address + 4)
        SetRegister(0);

      if (m_ram_select)
      {
        int address = (m_page_start_address + in_address) & RamAddressMask;
        address %= m_ram_size;

        return m_ram[address];
      }
      else
      {
        switch (m_rom_select)
        {
          case 0:
            return m_low_rom.MemoryRead(m_page_start_address + in_address);

          case 1:
            return m_high_rom.MemoryRead(m_page_start_address + in_address);

          default:
            return 0xff;
        }
      }
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      if (in_address >= m_register_start_address && in_address < m_register_start_address + 4)
      {
        SetRegister(in_byte);
      }
      else
      {
        if (m_ram_select)
        {
          int address = (m_page_start_address + in_address) & RamAddressMask;
          address %= m_ram_size;
            m_ram[address] = in_byte;
        }
        else
        {
          switch (m_rom_select)
          {
            case 0:
              m_low_rom.MemoryWrite(m_page_start_address + in_address, in_byte);
              if(m_low_rom.ROMContentChanged)
              {
                m_rom_content_changed = true;
                m_rom_changed_timestamp = DateTime.Now;
                m_low_rom.ROMContentChanged = false;
              }
              break;

            case 1:
              m_high_rom.MemoryWrite(m_page_start_address + in_address, in_byte);
              if (m_high_rom.ROMContentChanged)
              {
                m_rom_content_changed = true;
                m_rom_changed_timestamp = DateTime.Now;
                m_high_rom.ROMContentChanged = false;
              }
              break;
          }
        }
      }

      m_register_locked = in_address >= m_register_start_address + 2 && in_address < m_register_start_address + 4;
    }

    private void SetRegister(byte in_byte)
    {
      m_register = in_byte;

      m_ram_select = (m_register & (1 << 6)) != 0;
      m_register_start_address = ((m_register & (1 << 7)) != 0) ? 0x2000 : 0x0000;
      m_rom_select = (byte)((m_register >> 5) & 0x01);
      m_page_start_address = (m_register & 0x1f) * 0x4000;
    }

    public void Reset()
    {
      if (!m_register_locked)
        SetRegister(0);
    }

    public void Initialize(ITVComputer in_parent)
    {                        

    }

    public void Remove(ITVComputer in_parent)
    {

    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
      // save ROM content if enabled
      if(m_settings.AutosaveFlashContent && m_rom_content_changed && (DateTime.Now - m_rom_changed_timestamp).TotalMilliseconds>ROMSaveTimeout)
      {
        m_low_rom.Save();
        m_high_rom.Save();

        m_rom_content_changed = false;
      }
    }
  }
}
