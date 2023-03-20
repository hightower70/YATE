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
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using YATECommon;
using YATECommon.Helpers;

namespace NanoSD
{
  public class NanoSDCard : ITVCCard
  {
    public const int MaxRomSize = 64 * 1024;

    public byte[] Rom { get; private set; }

    private ExpansionMain m_expansion_main;
    private ITVComputer m_tvcomputer;
    private ArduinoCPU m_arduino_cpu;

    public NanoSDCardSettings Settings { get; private set; }
    public int ROMHighAddress { get; set; }

    public FileSystemWatcher m_file_system_watcher;

    public NanoSDCard(ExpansionMain in_expansion_main)
    {
      ROMHighAddress = 0xc000;

      m_expansion_main = in_expansion_main;

      m_arduino_cpu = new ArduinoCPU(this);
      m_file_system_watcher = new FileSystemWatcher();
      m_file_system_watcher.Created += FileSystemWatcherCreated;
      m_file_system_watcher.Deleted += FileSystemWatcherDeleted;
    }

    private void FileSystemWatcherDeleted(object sender, FileSystemEventArgs e)
    {
      m_arduino_cpu.FileSystemChanged(e.FullPath);
    }

    private void FileSystemWatcherCreated(object sender, FileSystemEventArgs e)
    {
      m_arduino_cpu.FileSystemChanged(e.FullPath);
    }

    public bool SetSettings(NanoSDCardSettings in_settings)
    {
      bool changed = false;
      byte[] old_rom = null;

      // save old rom
      old_rom = Rom;

      Settings = in_settings;

      // load ROM
      Rom = new byte[MaxRomSize];
      ROMFile.FillMemory(Rom);

      if (string.IsNullOrEmpty(Settings.ROMFileName))
      {
        ROMFile.LoadMemoryFromResource("NanoSD.Resources.NanoSDROM.bin", Rom);
      }
      else
      {
        ROMFile.LoadMemoryFromFile(Settings.ROMFileName, Rom);
      }

      changed = !ROMFile.IsMemoryEqual(old_rom, Rom);

      //LoadROMFromFile(m_settings.ROMFileName, 0);
      LoadCardRomFromResource("NanoSD.Resources.NanoSDROM.bin");

      // file system watcher
      if (Directory.Exists(Settings.FilesystemFolder))
      {
        m_file_system_watcher.EnableRaisingEvents = false;
        m_file_system_watcher.Path = Settings.FilesystemFolder;
        m_file_system_watcher.EnableRaisingEvents = true;
        m_arduino_cpu.FileSystemChanged(Settings.FilesystemFolder);
      }
      else
        m_file_system_watcher.EnableRaisingEvents = false;

      return changed;
    }

    public void StoreSettings()
    {
      m_expansion_main.ParentManager.Settings.SetSettings(Settings);
    }

    private void LoadROMFromFile(string in_rom_file_name, int in_address)
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

    /// <summary>
    /// Loads card ROM content from the given resource file
    /// </summary>
    /// <param name="in_resource_name"></param>
    public void LoadCardRomFromResource(string in_resource_name)
    {
      // load default key mapping
      Assembly assembly = Assembly.GetExecutingAssembly();

      using (Stream stream = assembly.GetManifestResourceStream(in_resource_name))
      {
        using (BinaryReader binary_reader = new BinaryReader(stream))
        {
          byte[] data = binary_reader.ReadBytes((int)stream.Length);

          int byte_to_copy = data.Length;

          if (byte_to_copy > Rom.Length)
            byte_to_copy = Rom.Length;

          Array.Copy(data, 0, Rom, 0, byte_to_copy);
        }
      }
    }

    public byte MemoryRead(ushort in_address)
    {
      return Rom[in_address | ROMHighAddress];
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
    }

    public void Reset()
    {
    }

    /*public void Initialize(ITVComputer in_parent)
    {
     
    }
      */
    public void PortRead(ushort in_address, ref byte inout_data)
    {
      inout_data = m_arduino_cpu.ReadByte();
    }

    public void PortWrite(ushort in_address, byte in_byte)
    {
      m_arduino_cpu.WriteByte(in_byte);
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
      // not needed
    }

    public byte GetID()
    {
      return 0x03;
    }

    public void Install(ITVComputer in_parent)
    {
      m_tvcomputer = in_parent;
    }

    public void Remove(ITVComputer in_parent)
    {
      // no action needed
    }

  }
}
