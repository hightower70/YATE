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
// Sound Magic main amulation class
///////////////////////////////////////////////////////////////////////////////
using YATECommon;
using YATECommon.Chips;
using YATECommon.Helpers;

namespace SoundMagic
{
  public class SoundMagicCard : ITVCCard
  {
    public const int RomSize = 32 * 1024;   // 32k is the max ROM size

    const uint SN_CLOCK = 3579545;
    const int YM_CLOCK = 3579545;

    private ITVComputer m_tvcomputer;
    private ExpansionMain m_expansion_main;

    private byte m_page_register = 0xff;
    private byte[] m_card_rom;

    public SoundMagicSettings Settings { get; private set; }

    private int m_audio_channel_index;

    private SN76489 m_SN76489;
    private SAA1099 m_SAA1099;
    private YM3812 m_YM3812;

    public SoundMagicCard(ExpansionMain in_expansion_main)
    {
      uint sample_rate = TVCManagers.Default.AudioManager.SampleRate;
      m_expansion_main = in_expansion_main;

      m_SN76489 = new SN76489();
      m_SN76489.Initialize(sample_rate, SN_CLOCK);

      m_SAA1099 = new SAA1099();
      m_SAA1099.Initialize(sample_rate);

      m_YM3812 = new YM3812();
      m_YM3812.Initialize(YM_CLOCK);
    }

    public bool SetSettings(SoundMagicSettings in_settings)
    {
      Settings = in_settings;

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
      byte[] old_rom = m_card_rom;       // save old rom content

      // load ROM
      m_card_rom = new byte[RomSize];
      ROMFile.FillMemory(m_card_rom);

      if (string.IsNullOrEmpty(Settings.ROMFileName))
      {
        ROMFile.LoadMemoryFromResource("SoundMagic.Resources.sndmx-fw-v1.0.1.bin", m_card_rom);
      }
      else
      {
        ROMFile.LoadMemoryFromFile(Settings.ROMFileName, m_card_rom);
      }

      changed = !ROMFile.IsMemoryEqual(old_rom, m_card_rom);

      return changed;
    }

    public byte MemoryRead(ushort in_address)
    {
      int address = (in_address & 0x1fff) + ((m_page_register & 0x03) << 13);

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
      m_page_register = 0xff;
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
          m_page_register = in_byte;
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

    public void Install(ITVComputer in_parent)
    {
      m_tvcomputer = in_parent;

      m_audio_channel_index = TVCManagers.Default.AudioManager.OpenChannel(RenderAudio);
    }

    public void Remove(ITVComputer in_parent)
    {
      TVCManagers.Default.AudioManager.CloseChannel(m_audio_channel_index);
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
  }
}
