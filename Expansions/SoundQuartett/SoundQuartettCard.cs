using System;
using System.IO;
using System.Reflection;
using YATECommon;
using YATECommon.Chips;

namespace SoundQuartett
{
  public class SoundQuartettCard : ITVCCard
  {
    const int CHANNEL_COUNT = 4;
    const uint MAIN_CLOCK = 119579;
    const int POST_SCALER_DIVISION = 10;
    const int POST_SCALER_HIGH = 8;

    private ITVComputer m_tvcomputer;
    private SoundQuartettSettings m_settings;
    private int m_audio_channel_index;
    private uint m_sample_rate;

    private uint m_clock_counter;

    private int[] m_channel_divisor;
    private int[] m_channel_counter;
    private int[] m_channel_post_scaler;
    private int[] m_channel_volume;

    private I8255 m_ppi1;
    private I8255 m_ppi2;

    /// <summary>Calculated DAC values from real resistors, maximizing at 8000</summary>
    private readonly int[] m_dac_values = new int[]
    {
      0,
      519,
      1092,
      1611,
      2129,
      2649,
      3222,
      3742,
      4260,
      4779,
      5352,
      5871,
      6389,
      6908,
      7481,
      8000
    };


    public SoundQuartettCard() 
    {
      m_channel_divisor = new int[CHANNEL_COUNT];
      m_channel_counter = new int[CHANNEL_COUNT];
      m_channel_volume = new int[CHANNEL_COUNT];
      m_channel_post_scaler = new int[CHANNEL_COUNT];


      m_ppi1 = new I8255();
      m_ppi2 = new I8255();

      // set event handlers
      m_ppi1.PortAChanged += PPI1PortAChanged;
      m_ppi1.PortBChanged += PPI1PortBChanged;
      m_ppi1.PortCChanged += PPI1PortCChanged;

      // set event handlers
      m_ppi2.PortAChanged += PPI2PortAChanged;
      m_ppi2.PortBChanged += PPI2PortBChanged;
      m_ppi2.PortCChanged += PPI2PortCChanged;

      m_sample_rate = TVCManagers.Default.AudioManager.SampleRate;
    }

    public void SetSettings(SoundQuartettSettings in_settings)
    {
      m_settings = in_settings;
    }

    public void Reset()
    {
      m_ppi1.Reset();
      m_ppi2.Reset();
    }

    public byte MemoryRead(ushort in_address)
    {
      // no memory
      return 0xff;
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // no memory
    }

    public void PortRead(ushort in_address, ref byte inout_data)
    {
    }

    public void PortWrite(ushort in_address, byte in_byte)
    {
      if ((in_address & 0x04) == 0)
      {
        m_ppi1.PortWrite((ushort)(in_address & 0x03), in_byte);
      }
      else
      {
        m_ppi2.PortWrite((ushort)(in_address & 0x03), in_byte);
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
    }

    public void Remove(ITVComputer in_parent)
    {
      TVCManagers.Default.AudioManager.CloseChannel(m_audio_channel_index);
    }

    private void PPI1PortAChanged(object sender, I8255.PortChangedEventArgs args)
    {
      if (m_ppi1.IsPortAOutput)
      {
        m_channel_divisor[0] = args.NewValue;
        AdvanceAudio();
      }
    }

    private void PPI1PortBChanged(object sender, I8255.PortChangedEventArgs args)
    {
      if (m_ppi1.IsPortBOutput)
      {
        m_channel_divisor[1] = args.NewValue;
        AdvanceAudio();
      }
    }

    private void PPI1PortCChanged(object sender, I8255.PortChangedEventArgs args)
    {
      if (m_ppi1.IsPortCLoOutput && m_ppi1.IsPortCHiOutput)
      {
        m_channel_divisor[2] = args.NewValue;
        AdvanceAudio();
      }
    }

    private void PPI2PortAChanged(object sender, I8255.PortChangedEventArgs args)
    {
      if (m_ppi2.IsPortAOutput)
      {
        m_channel_divisor[3] = args.NewValue;
        AdvanceAudio();
      }
    }

    private void PPI2PortBChanged(object sender, I8255.PortChangedEventArgs args)
    {
      if (m_ppi2.IsPortBOutput)
      {
        m_channel_volume[0] = m_dac_values[args.NewValue & 0x0f];
        m_channel_volume[1] = m_dac_values[(args.NewValue >> 4) & 0x0f];
        AdvanceAudio();
      }
    }

    private void PPI2PortCChanged(object sender, I8255.PortChangedEventArgs args)
    {
      if (m_ppi1.IsPortCLoOutput)
      {
        m_channel_volume[2] = m_dac_values[args.NewValue & 0x0f];
        AdvanceAudio();
      }

      if (m_ppi1.IsPortCHiOutput)
      {
        m_channel_volume[3] = m_dac_values[(args.NewValue >> 4) & 0x0f];
        AdvanceAudio();
      }
    }

    private void AdvanceAudio()
    {
      TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvcomputer.GetCPUTicks());
    }

    private void RenderAudio(int[] inout_buffer, int in_start_sample_index, int in_end_sample_index)
    {
      int sample_pos = in_start_sample_index * 2;
      ushort clock_cycles;
      int sample;

      // render samples
      for (int sample_index = in_start_sample_index; sample_index < in_end_sample_index; sample_index++)
      {
        m_clock_counter += MAIN_CLOCK;
        clock_cycles = (ushort)(m_clock_counter / m_sample_rate);
        m_clock_counter = m_clock_counter % m_sample_rate;

        sample = 0;
        for (int channel = 0; channel < CHANNEL_COUNT; channel++)
        {
          if (m_channel_divisor[channel] == 0 || m_channel_volume[channel] == 0)
            continue;

          m_channel_counter[channel] -= clock_cycles;

          while(m_channel_counter[channel] < 0)
          {
            // reload divisor
            m_channel_counter[channel] += m_channel_divisor[channel];

            // increment post scaler
            m_channel_post_scaler[channel]++;

            // reset postscaler
            if (m_channel_post_scaler[channel] >= POST_SCALER_DIVISION)
              m_channel_post_scaler[channel] -= POST_SCALER_DIVISION;
          }

          // determine output value
          if (m_channel_post_scaler[channel] >= POST_SCALER_HIGH)
            // output is high
            sample += m_channel_volume[channel];
          else
            // output is low
            sample -= m_channel_volume[channel];
        }

        // store sample
        inout_buffer[sample_pos++] += sample;
        inout_buffer[sample_pos++] += sample;
      }
    }
  }
}
