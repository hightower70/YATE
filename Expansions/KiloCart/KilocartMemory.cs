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
// 64kB ROM cartridge memory emulation class
///////////////////////////////////////////////////////////////////////////////
using System.Runtime.CompilerServices;
using YATECommon;
using YATECommon.Helpers;

namespace KiloCart
{
  /// <summary>
  /// Memory handler class
  /// </summary>
  internal class KilocartMemory : IDebuggableMemory
  {
    #region · Constants ·
    public const int ROMPageSize = 16384;
    public const int ROMPageCount = 4;
    public const int ROMSize = ROMPageSize * ROMPageCount;
    private readonly string[] ROMPageNames = { "ROM0", "ROM1", "ROM2", "ROM3" };
    #endregion

    #region · Data members ·
    private byte[] m_rom_memory;
    private byte m_page_register = 0;
    #endregion
 
    #region · Constuctor ·

    /// <summary>
    /// Default constructor
    /// </summary>
    public KilocartMemory()
    {
      m_rom_memory = new byte[ROMSize];
      ROMFile.FillMemory(m_rom_memory);

      m_page_register = 0;
    }
    #endregion

    #region · Pulic mebers · 

    /// <summary>
    /// Initializes memory content
    /// </summary>
    /// <param name="in_size">Size of the memory</param>
    /// <param name="in_rom_file_name">Name of the ROM content file</param>
    /// <returns></returns>
    public bool Initialize(string in_rom_file_name)
    {
      bool restart_tvc = false;

      // initialize members
      m_page_register = 0;

      // load ROM content
      ROMFile.LoadMemoryFromFile(in_rom_file_name, m_rom_memory, ref restart_tvc);

      return restart_tvc;
    }

    /// <summary>
    /// Sets page regoister value
    /// </summary>
    /// <param name="in_byte"></param>
    public void SetPageRegister(byte in_byte)
    {
      m_page_register = in_byte;
    }

    /// <summary>
    /// Reads memory content
    /// </summary>
    /// <param name="in_address">Address of the memory</param>
    /// <returns>Byte from the memory</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte MemoryRead(ushort in_address)
    {
      return m_rom_memory[in_address + (m_page_register << 14)];
    }

    #endregion

    #region · IDebuggableMemory support ·

    public TVCMemoryType MemoryType { get => TVCMemoryType.Cart; }

    public int AddressOffset { get => 0xc000; }

    public int MemorySize { get => m_rom_memory.Length; }

    public int PageCount { get => ROMPageCount; }

    public int PageIndex { get => m_page_register; }

    public string[] PageNames { get => ROMPageNames; }

    public byte DebugReadMemory(int in_page_index, int in_address)
    {
      return m_rom_memory[in_address + in_page_index * ROMPageSize];
    }

    public void DebugWriteMemory(int in_page_index, int in_address, byte in_data)
    {
      m_rom_memory[in_address + in_page_index * ROMPageSize] = in_data;
    }
    #endregion
  }
}
