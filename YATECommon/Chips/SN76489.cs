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
// SN76489 Sound Chip Emulation
///////////////////////////////////////////////////////////////////////////////
using System.Runtime.CompilerServices;

namespace YATECommon.Chips
{
  /// <summary>
  /// SN76489 Sound Chip Emulation Class
  /// </summary>
	public class SN76489
	{
		#region · Constants ·
		private const int CLOCK_DIVISOR = 16;
		private const int NOISE_INITIAL_STATE = 0x800;
		private const int NOISE_DEFAULT_TAP = 0x0009;
    private const int NUMBER_OF_REGISTERS = 8;
    private const int NUMBER_OF_CHANNELS = 4;
    private const int NUMBER_OF_TONE_CHANNELS = 3;

    // Aplitude table with 2db steps. Max amplitude is 8000
    private readonly ushort[] l_amplitude_table = { 8000, 6355, 5048, 4009, 3185, 2530, 2010, 1596, 1268, 1007, 800, 635, 505, 401, 318, 0 };

		#endregion

		#region · Data members ·
		private uint m_clock_frequency;
		private uint m_clock_counter;
    private uint m_sample_rate;

    private byte m_register_index;
		private ushort[] m_registers = new ushort[NUMBER_OF_REGISTERS];

		private ushort[] m_frequency = new ushort[NUMBER_OF_TONE_CHANNELS];
		private ushort[] m_counter = new ushort[NUMBER_OF_TONE_CHANNELS];
		private sbyte[] m_output = new sbyte[NUMBER_OF_TONE_CHANNELS];

		private ushort[] m_amplitude = new ushort[NUMBER_OF_CHANNELS];

    private byte m_noise_control;
    private ushort m_noise_counter;
    private sbyte m_noise_output;
    private ushort m_noise_shift_register;
    private ushort m_noise_tap;

		private short[] m_sample = new short[NUMBER_OF_CHANNELS];

    private bool m_stereo_mode = false;
    private sbyte[] m_paning = new sbyte[NUMBER_OF_CHANNELS]; // -127 ... 0 ... 127 (Left-Center-Right)
    #endregion

    #region · Public members ·

    /// <summary>
    /// Initializes sound chip
    /// </summary>
    /// <param name="in_sample_rate"></param>
    public void Initialize(uint in_sample_rate, uint in_clock_frequency)
		{
			m_sample_rate = in_sample_rate;
			m_clock_frequency = in_clock_frequency;
			Reset();
		}

		/// <summary>
		/// Resets SN76489
		/// </summary>
		public void Reset()
		{
			byte i;

			m_clock_counter = 0;

      // reset registers
      for (i = 0; i < m_registers.Length; i++)
				m_registers[i] = 0;

			for (i = 0; i < m_frequency.Length; i++)
				m_frequency[i] = 0;

			for (i = 0; i < m_counter.Length; i++)
				m_counter[i] = 0;

			for (i = 0; i < m_output.Length; i++)
				m_output[i] = 1;

			for (i = 0; i < m_amplitude.Length; i++)
				m_amplitude[i] = 0;

      for (i = 0; i < m_paning.Length; i++)
        m_paning[i] = 0;

      m_noise_shift_register = NOISE_INITIAL_STATE;
			m_noise_output = (sbyte)(m_noise_shift_register & 1);
			m_noise_tap = NOISE_DEFAULT_TAP;
			m_noise_control = 0;
			m_noise_counter = 0;
		}

    /// <summary>
    /// Writes SN76496 Register
    /// </summary>
    /// <param name="in_data">Data to write into the register</param>
    public void WriteRegister(byte in_data)
		{
			byte register_index;

			// determine register value
			if ((in_data & 0x80) != 0)
			{
				register_index = m_register_index = (byte)((in_data >> 4) & 0x07);
				m_registers[register_index] = (ushort)((m_registers[register_index] & 0x03f0) | (in_data & 0x0f));
			}
			else
			{
				register_index = m_register_index;
				m_registers[register_index] = (ushort)((m_registers[register_index] & 0x000f) | ((in_data & 0x3f) << 4));
			}

			// update register
			switch (register_index)
			{
				case 0: // tone 0: frequency
				case 2: // tone 1: frequency
				case 4: // tone 2: frequency
								// check for zero frequency
					m_frequency[register_index / 2] = m_registers[register_index];
					break;

				case 1: // tone 0: attenuation
				case 3: // tone 1: attenuation
				case 5: // tone 2: attenuation
				case 7: // Noise attenuation 
					m_amplitude[register_index / 2] = l_amplitude_table[in_data & 0x0f];
					break;

				case 6: // Noise control register
					m_noise_control = (byte)(in_data & 0x07);          // set noise register
					m_noise_shift_register = NOISE_INITIAL_STATE;       // reset shift register
					m_noise_output = (sbyte)(m_noise_shift_register & 1);  // set output
					break;
			}
		}

    /// <summary>
    /// Sets panning for stereo output
    /// </summary>
    /// <param name="in_channel">Channel index to change panning [0..3]</param>
    /// <param name="in_panning">Panning value [-127..127]</param>
    public void SetPanning(byte in_channel, sbyte in_panning)
		{
			m_paning[in_channel] = in_panning;
		}

    /// <summary>
    /// Renders audio stream
    /// </summary>
    /// <param name="out_left">Left channel output sample value</param>
    /// <param name="out_right">Right channel output sample value</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderAudioStream(ref int out_left, ref int out_right)
		{
			ushort sample_weight;
			ushort clock_cycles;
			ushort clock;
			ushort noise_divisor;
			byte noise_register_shift_count = 0;
			byte i;

			// update ClockCounter
			m_clock_counter += m_clock_frequency / CLOCK_DIVISOR;
			clock_cycles = (ushort)(m_clock_counter / m_sample_rate);
			m_clock_counter -= clock_cycles * m_sample_rate;

			// handle channels 0,1,2
			for (i = 0; i < 3; i++)
			{
				if (m_frequency[i] == 0)
				{
					m_output[i] = 1;
				}
				else
				{
					if (m_counter[i] < clock_cycles)
					{
						// run clock_cycle number of sound generarion cycle
						clock = clock_cycles;
						while (m_counter[i] < clock)
						{
							// update output
							m_output[i] = (sbyte)(-m_output[i]);

							// output for noise register
							if (i == 3)
								noise_register_shift_count++;

							// update counter
							clock -= m_counter[i];
							m_counter[i] = m_frequency[i];
						}
						m_counter[i] -= clock;
					}
					else
					{
						m_counter[i] -= (ushort)clock_cycles;
					}
				}

				// calculate sample
				m_sample[i] = (short)(m_output[i] * m_amplitude[i]);
			}

			// handle noise divisor (counter)
			if ((m_noise_control & 3) != 3)
			{
				noise_register_shift_count = 0;

				if (m_noise_counter < clock_cycles)
				{
					switch (m_noise_control & 3)
					{
						case 0:
							noise_divisor = 512;
							break;

						case 1:
							noise_divisor = 1024;
							break;

						case 2:
							noise_divisor = 2048;
							break;

						default:
							noise_divisor = 2048;
							break;
					}

					m_noise_counter = (ushort)(noise_divisor / CLOCK_DIVISOR + m_noise_counter - clock_cycles);
					noise_register_shift_count = 1;
				}
				else
				{
					m_noise_counter -= clock_cycles;
				}
			}

			// handle noise register
			while (noise_register_shift_count > 0)
			{
				// update shift register
				m_noise_shift_register = (ushort)((m_noise_shift_register >> 1) | ((((m_noise_control & 4) != 0) ? CalculateParity(m_noise_shift_register & m_noise_tap) : m_noise_shift_register & 1) << 15));

				noise_register_shift_count--;
			}

			// update output
			m_noise_output = (sbyte)(((m_noise_shift_register & 1) == 0) ? -1 : 1);

			// add noise output to the sample
			m_sample[3] = (short)(m_noise_output * m_amplitude[3]);

			// generate sample output
			if (m_stereo_mode)
			{
				// stereo output

				// left channel
				int sample_sum = 0;
				for (i = 0; i < 4; i++)
				{
					sample_weight = (ushort)(127 - m_paning[i]);
					if (sample_weight > 254)
						sample_weight = 254;

					sample_sum += m_sample[i] * sample_weight / 254;
				}
				out_left += sample_sum;

				// right channel
				sample_sum = 0;
				for (i = 0; i < 4; i++)
				{
					sample_weight = (ushort)(m_paning[i] + 127);
					if (sample_weight < 0)
						sample_weight = 0;

					sample_sum += (short)(m_sample[i] * sample_weight / 254);
				}
				out_right += sample_sum;
			}
			else
			{
				// mono output
				int sample_sum = m_sample[0] + m_sample[1] + m_sample[2] + m_sample[3];

				// sum of all channel
				out_left += sample_sum;
				out_right += sample_sum;
			}
		}

    /// <summary>
    /// Calculates parity of a ushort for noise generation
    /// </summary>
    /// <param name="in_value"></param>
    /// <returns></returns>
    private ushort CalculateParity(int in_value)
		{
			uint value = (uint)in_value;

			value ^= value >> 8;
			value ^= value >> 4;
			value ^= value >> 2;
			value ^= value >> 1;

			return (ushort)(value & 1);
		}

		#endregion
	}
}