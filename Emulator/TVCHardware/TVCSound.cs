///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2021 Laszlo Arvai. All rights reserved.
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
// TV Computer sound emulation
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Runtime.CompilerServices;
using YATE.Managers;
using YATECommon;

namespace YATE.Emulator.TVCHardware
{
  public class TVCSound
  {
    #region · Constants ·

    private const int CounterOverflowValue = 0x0FFF;
    private const int PostscalerOverflowValue = 0x0010;

    /// <summary>Calculated DAC values from real resistors, maximizing at 32700</summary>
    private readonly int[] m_dac_values = new int[]
    {
      0,
      2123,
      4464,
      6587,
      8704,
      10828,
      13168,
      15291,
      17409,
      19532,
      21873,
      23996,
      26113,
      28236,
      30577,
      32700
    };

    #endregion

    #region · Data members ·

    private TVComputer m_tvc;
    private int m_audio_channel_index;
    private int m_sample_rate;

    private int m_volume = 127; // current audio volume settings 0..127

    private int m_register;
    private int m_counter;
    private int m_counter_increment;
    private int m_counter_increment_remainder;
    private int m_counter_fraction;
    private int m_post_scaler;
    private ulong m_interrupt_timestamp;

    private int m_dac_value;

    private byte m_port04;
    private byte m_port05;
    private byte m_port06;

    #endregion

    #region · Constructor ·

    /// <summary>
    /// Derfault contructor
    /// </summary>
    /// <param name="in_tvc">Parent TVC class</param>
    public TVCSound(TVComputer in_tvc)
    {
      m_tvc = in_tvc;

      m_tvc.Ports.AddPortWriter(0x04, PortWrite04H);
      m_tvc.Ports.AddPortWriter(0x05, PortWrite05H);
      m_tvc.Ports.AddPortWriter(0x06, PortWrite06H);
      m_tvc.Ports.AddPortReader(0x5B, PortRead5BH);
      m_tvc.Ports.AddPortReset(0x05, PortReset05H);
      m_tvc.Ports.AddPortReset(0x06, PortReset06H);

      m_sample_rate = (int)TVCManagers.Default.AudioManager.SampleRate;

      m_counter_increment = m_tvc.CPUClock / m_sample_rate;
      m_counter_increment_remainder = m_tvc.CPUClock % m_sample_rate;

      m_audio_channel_index = TVCManagers.Default.AudioManager.OpenChannel(RenderAudio);
    }

    #endregion

    #region · Port read/write handlers ·

    /// <summary>
    /// Port 04h write
    /// </summary>
    /// <param name="in_address">Port address</param>
    /// <param name="in_data">Date to write to the port</param>
    // PORT 04H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |                   S O U N D  F R E Q U E N C Y                |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |   F7  |   F6  |   F5  |   F4  |   F3  |   F2  |   F1  |   F0  |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    private void PortWrite04H(ushort in_address, byte in_data)
    {
      TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvc.GetCPUTicks());

      m_port04 = in_data;

      m_register = (m_register & 0x0f00) | in_data;
    }

    /// <summary>
    /// Port 05h write
    /// </summary>
    /// <param name="in_address">Port address</param>
    /// <param name="in_data">Date to write to the port</param>
    // PORT 05H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |      S O U N D  F R E Q U E N C Y  A N D  C O N T R O L       |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |   +   |   +   |  IT   |  E/D  |  F11  |  F10  |   F9  |   F8  |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    private void PortWrite05H(ushort in_address, byte in_data)
    {
      TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvc.GetCPUTicks());

      m_port05 = in_data;

      m_register = (m_register & 0X00ff) | ((in_data << 8) & 0x0f00);
    }

    /// <summary>
    /// Port 05h reset
    /// </summary>
    private void PortReset05H()
    {
      TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvc.GetCPUTicks());

      m_port06 = 0;

      m_dac_value = m_dac_values[0];
    }

    /// <summary>
    /// Port 06h write
    /// </summary>
    /// <param name="in_address">Port address</param>
    /// <param name="in_data">Date to write to the port</param>
    // PORT 06H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |                   S O U N D  V O L U M E                      |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |   -   |   -   |   V3  |   V2  |   V1  |   V0  |   +   |   +   |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    private void PortWrite06H(ushort in_address, byte in_data)
    {
      TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvc.GetCPUTicks());

      m_port06 = in_data;

      m_dac_value = m_dac_values[(in_data >> 2) & 0xf];
    }

    /// <summary>
    /// Port 06h reset
    /// </summary>
    private void PortReset06H()
    {
      TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, m_tvc.GetCPUTicks());

      m_port06 = 0;

      m_dac_value = m_dac_values[0];
    }


    /// <summary>
    /// Reset sound frequency divisor counters
    /// </summary>
    /// <param name="in_port_address"></param>
    /// <param name="inout_data"></param>
    // PORT 06H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |                  R E S E T  C O U N T E R S                   |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |   -   |   -   |   -   |   -   |   -   |   -   |   -   |   -   |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    private void PortRead5BH(ushort in_port_address, ref byte inout_data)
    {
      ulong timestamp = m_tvc.GetCPUTicks();
      TVCManagers.Default.AudioManager.AdvanceChannel(m_audio_channel_index, timestamp);

      m_counter = m_register;
      m_post_scaler = 0;
      m_interrupt_timestamp = timestamp;
    }

    #endregion

    /// <summary>
    /// Renders audio samples into the audio buffer
    /// </summary>
    /// <param name="inout_buffer">Audio buffer</param>
    /// <param name="in_start_sample_index">Samples index of the first sample to render</param>
    /// <param name="in_end_sample_index">Samples index of the last sample to render</param>
    public void RenderAudio(int[] inout_buffer, int in_start_sample_index, int in_end_sample_index)
    {
      int sample_pos = in_start_sample_index * 2;
      int sample;
      int dac_value = m_dac_value * m_volume / 128;
      int sample_index;

      // render samples
      for (sample_index = in_start_sample_index; sample_index < in_end_sample_index; sample_index++)
      {
        // increment counter
        m_counter += m_counter_increment;
        m_counter_fraction += m_counter_increment_remainder;
        if(m_counter_fraction >= m_sample_rate)
        {
          m_counter_fraction -= m_sample_rate;
          m_counter++;
        }

        // check for counter overflow
        while(m_counter >= CounterOverflowValue)
        {
          if (CounterOverflowValue == m_register)
          {
            m_counter = 0;
          }
          else
          {
            m_counter = m_counter - CounterOverflowValue + m_register;

            // poscaler increment
            m_post_scaler++;
            if (m_post_scaler >= PostscalerOverflowValue)
            {
              m_post_scaler -= PostscalerOverflowValue;
            }
          }
        }

        // get current sample
        if ((m_port05 & 0x10) != 0)
        {
          if ((m_post_scaler & 0x0008) == 0)
            sample = -dac_value;
          else
            sample = dac_value;
        }
        else
        {
          sample = dac_value;
        }

        // store calculated sample on both (L,R) channels
        inout_buffer[sample_pos++] += sample;
        inout_buffer[sample_pos++] += sample;
      }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PeriodicCallback()
    {
      ulong timer_overflow_tick_count = (ulong)((CounterOverflowValue - m_register) * 16);

      // check for interrupt enable
      if ((m_port05 & 0x20) != 0)
      {
        // check for overflow
        ulong ellapsed_period = m_tvc.GetTicksSince(m_interrupt_timestamp);
        if (ellapsed_period > timer_overflow_tick_count)
        {
          m_tvc.Interrupt.GenerateCursorSoundInterrupt();

          m_interrupt_timestamp += (ellapsed_period / timer_overflow_tick_count) * timer_overflow_tick_count;// m_tvc.CPU.TotalTState;// (ellapsed_period / timer_overflow_tick_count+ 1) * timer_overflow_tick_count; //m_tvc.CPU.TotalTState;// ellapsed_period - timer_overflow_tick_count;
        }
      }
    }
  }
}
