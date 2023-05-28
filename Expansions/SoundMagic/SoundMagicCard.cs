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
// Sound Magic main emulation class
///////////////////////////////////////////////////////////////////////////////
using System;
using YATECommon;
using YATECommon.Chips;
using YATECommon.Helpers;

namespace SoundMagic
{
  public class SoundMagicCard : ITVCCard, IDebuggableMemory
  {
    #region · Constants ·
    const uint SN_CLOCK = 3579545;
    const int YM_CLOCK = 3579545;

    public const int RomPageCount = 4;
    public const int RomPageSize = 8192;
    public const int RomSize = RomPageCount * RomPageSize;

    #endregion

    #region · Data members ·

    private ITVComputer m_tvcomputer;
    private ExpansionMain m_expansion_main;

    private byte m_page_register = 0xff;
    private byte[] m_card_rom;
    private int m_page_index;
    private int m_page_address;

    private int m_audio_channel_index;

    private SN76489 m_SN76489;
    private SAA1099 m_SAA1099;
    private YM3812 m_YM3812;

    private readonly string[] m_rom_page_names;

    #endregion

    #region · Properties ·
    public SoundMagicSettings Settings { get; private set; }

    #endregion

    #region · Constructor ·
    public SoundMagicCard(ExpansionMain in_expansion_main)
    {
      uint sample_rate = TVCManagers.Default.AudioManager.SampleRate;
      m_expansion_main = in_expansion_main;

      // create ROM
      m_card_rom = new byte[RomSize];
      ROMFile.FillMemory(m_card_rom);

      m_SN76489 = new SN76489();
      m_SN76489.Initialize(sample_rate, SN_CLOCK);

      m_SAA1099 = new SAA1099();
      m_SAA1099.Initialize(sample_rate);

      m_YM3812 = new YM3812();
      m_YM3812.Initialize(YM_CLOCK);

      string[] rom_page_names = new string[RomPageCount];
      for (int i = 0; i < rom_page_names.Length; i++)
      {
        rom_page_names[i] = String.Format("ROM[{0}]", i);
      }
      m_rom_page_names = rom_page_names;
    }
    #endregion

    public bool SetSettings(SoundMagicSettings in_settings)
    {
      Settings = in_settings;

      MemoryType = in_settings.GetCardMemoryType();

      // load ROM content
      bool settings_changed = LoadROM();

      return settings_changed;
    }

    public void StoreSettings()
    {
      m_expansion_main.ParentManager.Settings.SetSettings(Settings);
    }

    /// <summary>
    /// Loads ROM content of the file
    /// </summary>
    /// <returns></returns>
    private bool LoadROM()
    {
      bool changed = false;

      // load ROM
      if (string.IsNullOrEmpty(Settings.ROMFileName))
      {
        ROMFile.LoadMemoryFromResource("SoundMagic.Resources.sndmx-fw-v1.0.1.bin", m_card_rom, ref changed);
      }
      else
      {
        ROMFile.LoadMemoryFromFile(Settings.ROMFileName, m_card_rom, ref changed);
      }

      return changed;
    }

    private void SetPageSelection(byte in_register_value)
    {
      m_page_register = in_register_value;
      m_page_index = m_page_register & 0x03;
      m_page_address = m_page_index * RomPageSize;
    }

    public byte MemoryRead(ushort in_address)
    {
      int address = (in_address & 0x1fff) + m_page_address;

      if (address < m_card_rom.Length)
        return m_card_rom[address];
      else
        return 0xff;
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // no memory write
    }

    public void Reset()
    {
      SetPageSelection(0xff);
    }

    public void PortRead(ushort in_address, ref byte inout_data)
    {
      inout_data = 0xff; // no port read
    }

    public void PortWrite(ushort in_address, byte in_byte)
    {
      switch(in_address & 0x0f)
      {
        case 0:
        case 1:
          TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvcomputer.GetCPUTicks());
          m_SN76489.WriteRegister(in_byte);
          break;

        case 2:
          TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvcomputer.GetCPUTicks());
          m_SAA1099.WriteControlRegister(in_byte);
          break;

        case 3:
          TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvcomputer.GetCPUTicks());
          m_SAA1099.WriteAddressRegister(in_byte);
          break;

        case 4:
          m_YM3812.WriteControlRegister(in_byte);
          break;

        case 5:
          TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvcomputer.GetCPUTicks());
          m_YM3812.WriteDataRegister(in_byte);
          break;

        case 6:
        case 7:
          SetPageSelection(in_byte);
          break;
      }
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
    }

    public byte GetID()
    {
      return 0x03; // no ID
    }

    public void Insert(ITVComputer in_parent)
    {
      m_tvcomputer = in_parent;

      m_audio_channel_index = TVCManagers.Default.AudioManager.OpenChannel(RenderAudio);

      TVCManagers.Default.DebugManager.RegisterDebuggableMemory(this);
    }

    public void Remove(ITVComputer in_parent)
    {
      TVCManagers.Default.AudioManager.CloseChannel(m_audio_channel_index);

      TVCManagers.Default.DebugManager.UnregisterDebuggableMemory(this);
    }


    private void RenderAudio(int[] inout_buffer, int in_start_sample_index, int in_end_sample_index)
    {
      int left_sample = 0, right_sample = 0;
      int sample_pos = in_start_sample_index * 2;

      // render samples
      for (int sample_index = in_start_sample_index; sample_index < in_end_sample_index; sample_index++)
      {
        left_sample = 0;
        right_sample = 0;

        m_SN76489.RenderAudioStream(ref left_sample, ref right_sample);
        m_SAA1099.RenderAudioStream(ref left_sample, ref right_sample);
        m_YM3812.RenderAudioStream(ref left_sample, ref right_sample);

        inout_buffer[sample_pos++] += left_sample / 3;
        inout_buffer[sample_pos++] += right_sample / 3;
      }
    }

    #region · IDebuggableMemory implementation ·
    public TVCMemoryType MemoryType { get; private set; }

    public int AddressOffset { get { return 0xe000; } }

    public int MemorySize { get { return RomPageSize; } }

    public int PageCount { get { return RomPageCount; } }

    public int PageIndex { get { return m_page_index; } }

    public string[] PageNames { get { return m_rom_page_names; } }
    public byte DebugReadMemory(int in_page_index, int in_address)
    {
      return m_card_rom[in_page_index * RomPageSize + in_address];
    }

    public void DebugWriteMemory(int in_page_index, int in_address, byte in_data)
    {
      m_card_rom[in_page_index * RomPageSize + in_address] = in_data;
    }

    #endregion
  }
}
