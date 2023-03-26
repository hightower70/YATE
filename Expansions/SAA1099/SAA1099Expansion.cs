using System;
using YATECommon;
using YATECommon.Chips;

namespace MemoryExpansion32k
{
  public class SAA1099Expansion
  {
    private SAA1099 m_sound_chip;
    private int m_audio_channel_index;
    private ITVComputer m_tvcomputer;

    public void Install(ITVComputer in_computer)
    {
      m_tvcomputer = in_computer;

      m_sound_chip = new SAA1099();
      m_sound_chip.Initialize(TVCManagers.Default.AudioManager.SampleRate);

      in_computer.Ports.AddPortWriter(0xfe, PortWriteFEH);
      in_computer.Ports.AddPortWriter(0xff, PortWriteFFH);

      m_audio_channel_index = TVCManagers.Default.AudioManager.OpenChannel(RenderAudio);

    }

    public void Remove(ITVComputer in_computer)
    {
      TVCManagers.Default.AudioManager.CloseChannel(m_audio_channel_index);

      in_computer.Ports.RemovePortWriter(0xfe, PortWriteFEH);
      in_computer.Ports.RemovePortWriter(0xff, PortWriteFFH);

      m_audio_channel_index = -1;
      m_sound_chip = null;
    }

    private void PortWriteFEH(ushort in_port_address, byte in_data)
    {
      TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvcomputer.GetCPUTicks());
      m_sound_chip.WriteControlRegister(in_data);
    }

    private void PortWriteFFH(ushort in_port_address, byte in_data)
    {
      TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvcomputer.GetCPUTicks());
      m_sound_chip.WriteAddressRegister(in_data);
    }

    private void RenderAudio(int[] inout_buffer, int in_start_sample_index, int in_end_sample_index)
    {
      int sample_pos = in_start_sample_index * 2;

      // render samples
      for (int sample_index = in_start_sample_index; sample_index < in_end_sample_index; sample_index++)
      {
        m_sound_chip.RenderAudioStream(ref inout_buffer[sample_pos], ref inout_buffer[sample_pos+1]);
        sample_pos += 2;
      }
    }
  }
}
