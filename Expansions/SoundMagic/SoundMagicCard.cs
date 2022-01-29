using System;
using System.IO;
using System.Reflection;
using YATECommon;
using YATECommon.Chips;

namespace SoundMagic
{
  public class SoundMagicCard : ITVCCard
  {
    public const int RomSize = 32 * 1024;   // 32k is the max ROM size

    const uint SN_CLOCK = 3579545;
    const int YM_CLOCK = 3579545;

    private ITVComputer m_tvcomputer;
    private SoundMagicSettings m_settings;

    private byte m_page_register = 0xff;
    private byte[] m_card_rom;

    private int m_audio_channel_index;

    private SN76489 m_SN76489;
    private SAA1099 m_SAA1099;
    private YM3812 m_YM3812;

    public SoundMagicCard() 
    {
      uint sample_rate = TVCManagers.Default.AudioManager.SampleRate;

      m_SN76489 = new SN76489();
      m_SN76489.Initialize(sample_rate, SN_CLOCK);

      m_SAA1099 = new SAA1099();
      m_SAA1099.Initialize(sample_rate);

      m_YM3812 = new YM3812();
      m_YM3812.Initialize(YM_CLOCK);
    }

    public void SetSettings(SoundMagicSettings in_settings)
    {
      m_settings = in_settings;

      // load ROM
      m_card_rom = new byte[RomSize];
      for (int i = 0; i < m_card_rom.Length; i++)
        m_card_rom[i] = 0xff;

      LoadCardRomFromResource("SoundMagic.Resources.sndmx-fw-v1.0.1.bin");
    }

    private void LoadROMContent(string in_rom_file_name)
    {
      if (!string.IsNullOrEmpty(in_rom_file_name))
      {
        byte[] data = File.ReadAllBytes(in_rom_file_name);

        int length = data.Length;

        if (data.Length > m_card_rom.Length)
          length = m_card_rom.Length;

        Array.Copy(data, 0, m_card_rom, 0, length);
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

          if (byte_to_copy > m_card_rom.Length)
            byte_to_copy = m_card_rom.Length;

          Array.Copy(data, 0, m_card_rom, 0, byte_to_copy);
        }
      }
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
