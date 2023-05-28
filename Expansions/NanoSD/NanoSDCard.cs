///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2021 Laszlo Arvai. All rights reserved.
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
// NanoSD card emulation class
///////////////////////////////////////////////////////////////////////////////
using System.IO;
using System.Runtime.CompilerServices;
using YATECommon;
using YATECommon.Helpers;

namespace NanoSD
{
  public class NanoSDCard : ITVCCard, IDebuggableMemory
  {
    #region · Constants ·

    public const int RomPageCount = 2;
    public const int RomPageSize = 8192;
    public const int MaxRomSize = 64 * 1024;

    #endregion

    #region · Data members ·

    private ExpansionMain m_expansion_main;
    private ITVComputer m_tvcomputer;
    private ArduinoCPU m_arduino_cpu;
    private int m_memory_page_index = 0;
    private ushort m_memory_high_address = 0;

    private FileSystemWatcher m_file_system_watcher;

    private readonly string[] m_rom_page_names;
    #endregion

    #region · Properties ·
    public byte[] Rom { get; private set; }

    public NanoSDCardSettings Settings { get; private set; }


    #endregion

    #region · Constructor · 
    public NanoSDCard(ExpansionMain in_expansion_main)
    {
      m_expansion_main = in_expansion_main;
      m_memory_page_index = 0;
      m_memory_high_address = 0xc000;

      Rom = new byte[MaxRomSize];
      ROMFile.FillMemory(Rom);

      m_arduino_cpu = new ArduinoCPU(this);
      m_file_system_watcher = null;

      m_rom_page_names = new string[] { "ROM[0]", "ROM[1]" };
    }

    #endregion

    private void FileSystemWatcherDeleted(object sender, FileSystemEventArgs e)
    {
      m_arduino_cpu.FileSystemChanged(e.FullPath);
    }

    private void FileSystemWatcherCreated(object sender, FileSystemEventArgs e)
    {
      m_arduino_cpu.FileSystemChanged(e.FullPath);
    }

    internal void SetMemoryPage(int in_index)
    {
      m_memory_page_index = in_index;
      m_memory_high_address = (ushort)(0xc000 + in_index * RomPageSize);
    }

    public bool SetSettings(NanoSDCardSettings in_settings)
    {
      bool changed = false;

      // store settings
      Settings = in_settings;

      MemoryType = in_settings.GetCardMemoryType();

      // load ROM
      if (string.IsNullOrEmpty(Settings.ROMFileName))
      {
        ROMFile.LoadMemoryFromResource("NanoSD.Resources.NanoSDROM-v0.33.bin", Rom, ref changed);
      }
      else
      {
        ROMFile.LoadMemoryFromFile(Settings.ROMFileName, Rom, ref changed);
      }

      // update file system folder
      UpdateFileSystemWatcher();
      m_arduino_cpu.FileSystemChanged(Settings.FilesystemFolder);

      return changed;
    }

    public void StoreSettings()
    {
      m_expansion_main.ParentManager.Settings.SetSettings(Settings);
    }

    private void UpdateFileSystemWatcher()
    {
      if (m_file_system_watcher != null)
      {
        if (Directory.Exists(Settings.FilesystemFolder))
        {
          m_file_system_watcher.EnableRaisingEvents = false;
          m_file_system_watcher.Path = Settings.FilesystemFolder;
          m_file_system_watcher.EnableRaisingEvents = true;
        }
        else
          m_file_system_watcher.EnableRaisingEvents = false;
      }
    }


    #region · ITVCCard implementation ·

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte MemoryRead(ushort in_address)
    {
      return Rom[in_address | m_memory_high_address];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // no write
    }

    public void Reset()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PortRead(ushort in_address, ref byte inout_data)
    {
      inout_data = m_arduino_cpu.ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PortWrite(ushort in_address, byte in_byte)
    {
      m_arduino_cpu.WriteByte(in_byte);
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
      // not needed
    }

    /// <summary>
    /// Gets card HW ID
    /// </summary>
    /// <returns></returns>
    public byte GetID()
    {
      return 0x03;
    }

    /// <summary>
    /// Installs card to the computer
    /// </summary>
    /// <param name="in_parent"></param>
    public void Insert(ITVComputer in_parent)
    {
      m_tvcomputer = in_parent;

      m_file_system_watcher = new FileSystemWatcher();
      m_file_system_watcher.Created += FileSystemWatcherCreated;
      m_file_system_watcher.Deleted += FileSystemWatcherDeleted;

      UpdateFileSystemWatcher();

      TVCManagers.Default.DebugManager.RegisterDebuggableMemory(this);
    }

    /// <summary>
    /// Removes card from the computer
    /// </summary>
    /// <param name="in_parent"></param>
    public void Remove(ITVComputer in_parent)
    {
      if (m_file_system_watcher != null)
      {
        m_file_system_watcher.EnableRaisingEvents = false;

        m_file_system_watcher.Created -= FileSystemWatcherCreated;
        m_file_system_watcher.Deleted -= FileSystemWatcherDeleted;

        m_file_system_watcher.Dispose();

        m_file_system_watcher = null;
      }

      TVCManagers.Default.DebugManager.UnregisterDebuggableMemory(this);
    }
    #endregion

    #region · IDebuggableMemory implementation ·
    public TVCMemoryType MemoryType { get; private set; }

    public int AddressOffset { get { return 0xe000; } }

    public int MemorySize { get { return RomPageSize; } }

    public int PageCount { get { return RomPageCount; } }

    public int PageIndex { get { return m_memory_page_index; } }

    public string[] PageNames { get { return m_rom_page_names; } }
    public byte DebugReadMemory(int in_page_index, int in_address)
    {
      return Rom[in_page_index * RomPageSize + in_address + 0xc000];
    }

    public void DebugWriteMemory(int in_page_index, int in_address, byte in_data)
    {
      Rom[in_page_index * RomPageSize + in_address + 0xc000] = in_data;
    }

    #endregion
  }
}
