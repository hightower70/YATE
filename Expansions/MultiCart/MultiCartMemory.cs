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
// Multicart Memory Emulation
///////////////////////////////////////////////////////////////////////////////
using MultiCart;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using YATECommon;
using YATECommon.Chips;

namespace MultiCart
{
  internal class MultiCartMemory : IDebuggableMemory
  {
    #region · Constants ·
    public const int PageLength = 16384;
    #endregion

    #region · Data members ·

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
    private int m_ram_page_count;
    private int m_rom_page_count;

    private bool m_rom_content_changed;
    private DateTime m_rom_changed_timestamp;


    #endregion

    #region · Properties ·
    public bool IsRegisterLocked { get { return m_register_locked; } }

    public bool IsROMContentChanged { get { return m_rom_content_changed; } }

    public DateTime ROMContentChangedTimestamp { get { return m_rom_changed_timestamp; } }
    #endregion

    /// <summary>
    /// Default cosntructor
    /// </summary>
    public MultiCartMemory()
    {
      m_high_rom = new SST39SF040();
      m_low_rom = new SST39SF040();
      m_rom_page_count = (SST39SF040.FlashSize + SST39SF040.FlashSize) / PageLength;

      m_register = 0;
      m_page_start_address = 0;
      m_rom_select = 0;
      m_register_start_address = 0;
      m_ram_select = false;
      m_register_locked = false;
      m_rom_content_changed = false;
    }

    /// <summary>
    /// Memory read function
    /// </summary>
    /// <param name="in_address">Address of the memory to read</param>
    /// <returns>Data from the memory at the given address</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte MemoryRead(ushort in_address)
    {
      if (!m_register_locked && in_address >= m_register_start_address && in_address <= m_register_start_address + 3)
        SetRegister(0);

      if (m_ram_select)
      {
        int address = (m_page_start_address + in_address) % m_ram_size;

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

    /// <summary>
    /// Write data to the memory
    /// </summary>
    /// <param name="in_address">Address to write to</param>
    /// <param name="in_byte">Data to write</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      if (in_address >= m_register_start_address && in_address <= m_register_start_address + 3)
      {
        SetRegister(in_byte);
      }
      else
      {
        if (m_ram_select)
        {
          int address = (m_page_start_address + in_address) % m_ram_size;

          m_ram[address] = in_byte;
        }
        else
        {
          switch (m_rom_select)
          {
            case 0:
              m_low_rom.MemoryWrite(m_page_start_address + in_address, in_byte);
              if (m_low_rom.ROMContentChanged)
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

      m_register_locked = (in_address >= m_register_start_address + 2 && in_address <= m_register_start_address + 3);
    }

    /// <summary>
    /// Sets paging register content
    /// </summary>
    /// <param name="in_byte"></param>
    public void SetRegister(byte in_byte)
    {
      m_register = in_byte;

      m_ram_select = (m_register & (1 << 6)) != 0;
      m_register_start_address = ((m_register & (1 << 7)) != 0) ? 0x2000 : 0x0000;
      m_rom_select = (byte)((m_register >> 5) & 0x01);
      m_page_start_address = (m_register & 0x1f) * 0x4000;
    }

    /// <summary>
    /// Initializes card memories
    /// </summary>
    /// <param name="in_rom1_filename"></param>
    /// <param name="in_rom2_filename"></param>
    /// <param name="in_ram_size"></param>
    /// <returns></returns>
    public bool Initialize(string in_rom1_filename, string in_rom2_filename, int in_ram_size)
    {
      bool restart_tvc = false;

      // load ROM content
      m_low_rom.ROMFileName = in_rom1_filename;
      if (m_low_rom.Load())
        restart_tvc = true;

      m_high_rom.ROMFileName = in_rom2_filename;
      if (m_high_rom.Load())
        restart_tvc = true;

      // init RAM
      m_ram = new byte[in_ram_size];
      m_ram_size = in_ram_size;
      m_ram_page_count = m_ram_size / 16384;

      // init registers
      m_register = 0;
      m_page_start_address = 0;
      m_rom_select = 0;
      m_register_start_address = 0;
      m_ram_select = false;
      m_register_locked = false;
      m_rom_content_changed = false;


      return restart_tvc;
    }

    /// <summary>
    /// Save ROM content
    /// </summary>
    public void SaveROMContent()
    {
      m_low_rom.Save();
      m_high_rom.Save();

      m_rom_content_changed = false;
    }

    #region · IDebuggableMemory members · 

    public int AddressOffset
    {
      get
      {
        return 0xc000;
      }
    }

    public int MemorySize
    {
      get
      {
        return PageLength;
      }
    }

    public int PageCount
    {
      get
      {
        return m_rom_page_count + m_ram_page_count;
      }
    }

    public int PageIndex
    {
      get
      {
        if(m_ram_select)
        {
          return m_rom_page_count + (m_register & 0x1f) % m_ram_page_count;
        }
        else
        {
          return (m_register & 0x1f) % m_rom_page_count;
        }
      }
    }

    public string[] PageNames
    {
      get
      {
        string[] names = new string[m_rom_page_count + m_ram_page_count];

        int i;
        for (i = 0; i < m_rom_page_count; i++)
        {
          names[i] = "ROM[" + i.ToString("X2") + "]";
        }
        for (i = 0; i < m_ram_page_count; i++)
        {
          names[m_rom_page_count + i] = "RAM[" + i.ToString("X2") + "]";
        }

        return names;
      }
    }


    public TVCMemoryType MemoryType
    {
      get => TVCMemoryType.Cart;
    }


    public byte DebugReadMemory(int in_page_index, int in_address)
    {
      if (in_page_index < m_rom_page_count)
      {
        // ROM page requested
        int address = (in_page_index * PageLength + in_address) % (SST39SF040.FlashSize + SST39SF040.FlashSize);

        if (address < SST39SF040.FlashSize)
          return m_low_rom.MemoryRead(address);
        else
          return m_low_rom.MemoryRead(address - SST39SF040.FlashSize);
      }
      else
      {
        // RAM page requested
        int address = ((in_page_index - m_rom_page_count) * PageLength + in_address) % m_ram_size;

        return m_ram[address];
      }

    }

    public void DebugWriteMemory(int in_page_index, int in_address, byte in_data)
    {
      if (in_page_index < m_rom_page_count)
      {
        // ROM page requested
        int address = (in_page_index * PageLength + in_address) % (SST39SF040.FlashSize + SST39SF040.FlashSize);

        if (address < SST39SF040.FlashSize)
          m_low_rom.MemoryWrite(address, in_data);
        else
          m_low_rom.MemoryWrite(address - SST39SF040.FlashSize, in_data);
      }
      else
      {
        // RAM page requested
        int address = ((in_page_index - m_rom_page_count) * PageLength + in_address) % m_ram_size;

        m_ram[address] = in_data;
      }
    }


    #endregion
  }
}
