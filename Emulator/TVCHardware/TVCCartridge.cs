///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2020 Laszlo Arvai. All rights reserved.
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
// Videoton TV Computer ROM Cartridge Emulation
///////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using YATE.Settings;
using YATECommon;
using YATECommon.Settings;

namespace YATE.Emulator.TVCHardware
{
  /// <summary>
  /// Class for emulating TVC cartridge interface
  /// </summary>
  class TVCCartridge : ITVCCartridge
  {
    #region  · Constants ·
    public const int CartMemLength = 0x4000;
    #endregion

    #region  · Properties ·
    /// <summary>
    /// Cartidge memmory content
    /// </summary>
    public byte[] Memory { get; private set; }
    #endregion

    /// <summary>
    /// Reads cartridge memory by CPU
    /// </summary>
    /// <param name="in_address">Address ro read from</param>
    /// <returns>Data byte at the given address</returns>
    public byte MemoryRead(ushort in_address)
    {
      return Memory[in_address];
    }

    /// <summary>
    /// Writes cartridge memory (cartridge is read-only)
    /// </summary>
    /// <param name="in_address">Address to write</param>
    /// <param name="in_byte">Data to write</param>
    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // no write
    }

    public void Reset()
    {
      // no reset
    }

    /// <summary>
    /// Initializes cartridge
    /// </summary>
    /// <param name="in_parent">TVC hardware class</param>
    public void Initialize(ITVComputer in_parent)
    {
      // allocate cartridge memory
      Memory = new byte[CartMemLength];

      // load cartridge content
      try
      {
        TVCConfigurationSettings settings = SettingsFile.Default.GetSettings<TVCConfigurationSettings>();

        if (settings != null && !string.IsNullOrEmpty(settings.CartridgeFileName) && settings.CartridgeActive)
          ReadCartridgeFile(settings.CartridgeFileName);
      }
      catch
      {
        ClearCartridge();
      }
    }

    /// <summary>
    /// Clears cartridge content
    /// </summary>
    public void ClearCartridge()
    {
      // if load failed -> init cartridge
      for (int i = 0; i < Memory.Length; i++)
        Memory[i] = 0xff;
    }

    /// <summary>
    /// Removes cartridge
    /// </summary>
    public void Remove(ITVComputer in_parent)
    {
      // non relevant
    }

    /// <summary>
    /// Reads cartrige content from a file
    /// </summary>
    /// <param name="in_file_name"></param>
    public void ReadCartridgeFile(string in_file_name)
    {
      long file_length = 0;

      if (!string.IsNullOrEmpty(in_file_name))
      {
        // load cart content
        FileInfo file = new FileInfo(in_file_name);
        file_length = file.Length;

        // maximize length up to the cartidge size
        if (file_length > TVCCartridge.CartMemLength)
          file_length = TVCCartridge.CartMemLength;

        // load file
        byte[] file_data;

        using (FileStream fs = File.OpenRead(in_file_name))
        {
          using (BinaryReader binaryReader = new BinaryReader(fs))
          {
            file_data = binaryReader.ReadBytes((int)fs.Length);
          }
        }

        // copy content to the memory
        Array.Copy(file_data, 0, Memory, 0, file_length);
      }

      // clear remaining memory
      for (int i = (int)file_length; i < TVCCartridge.CartMemLength; i++)
      {
        Memory[i] = 0xff;
      }
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    { 
      // non relevant
    }
  }
}
