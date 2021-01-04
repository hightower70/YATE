/*****************************************************************************/
/* SN76489 Sound Chip Emulation                                              */
/*                                                                           */
/* Copyright (C) 2013 Laszlo Arvai                                           */
/* All rights reserved.                                                      */
/*                                                                           */
/* This software may be modified and distributed under the terms             */
/* of the BSD license.  See the LICENSE file for details.                    */
/*****************************************************************************/

namespace YATECommon.Chips
{
	class SN76489
	{
		#region · Constants ·
		private const int CLOCK_DIVISOR = 16;
		private const int NOISE_INITIAL_STATE = 0x800;
		private const int NOISE_DEFAULT_TAP = 0x0009;

		// Aplitude table with 2db steps. Max amplitude is 8000
		private readonly ushort[] l_amplitude_table = { 8000, 6355, 5048, 4009, 3185, 2530, 2010, 1596, 1268, 1007, 800, 635, 505, 401, 318, 0 };

		#endregion

		#region · Data members ·
		private uint ClockFrequency;
		private uint ClockCounter;

		private byte RegisterIndex;
		private byte[] Registers = new byte[8];

		private sbyte[] Paning = new sbyte[4]; // -127 ... 0 ... 127 (Left-Center-Right)

		private ushort[] Frequency = new ushort[3];
		private ushort[] Counter = new ushort[3];
		private sbyte[] Output = new sbyte[3];
		private ushort[] Amplitude = new ushort[4];
		private byte NoiseControl;
		private ushort NoiseCounter;
		private sbyte NoiseOutput;
		private ushort NoiseShiftRegister;
		private ushort NoiseTap;
		private uint m_sample_rate;
		private short[] m_sample = new short[4];
		private bool m_stereo_mode = false;
		#endregion

		#region · Public members ·

		/// <summary>
		/// Initializes sound chip
		/// </summary>
		/// <param name="in_sample_rate"></param>
		public void Initialize(uint in_sample_rate, uint in_clock_frequency)
		{
			m_sample_rate = in_sample_rate;
			ClockFrequency = in_clock_frequency;
			Reset();
		}

		/// <summary>
		/// Resets SN76489
		/// </summary>
		void Reset()
		{
			byte i;

			ClockCounter = 0;

			for (i = 0; i < Registers.Length; i++)
				Registers[i] = 0;

			for (i = 0; i < Frequency.Length; i++)
				Frequency[i] = 1;

			for (i = 0; i < Counter.Length; i++)
				Counter[i] = 0;

			for (i = 0; i < Output.Length; i++)
				Output[i] = 1;

			for (i = 0; i < Amplitude.Length; i++)
			{
				Amplitude[i] = 0;
				Paning[i] = 0;
			}

			NoiseShiftRegister = NOISE_INITIAL_STATE;
			NoiseOutput = (sbyte)(NoiseShiftRegister & 1);
			NoiseTap = NOISE_DEFAULT_TAP;
			NoiseControl = 0;
			NoiseCounter = 0;
		}

		///////////////////////////////////////////////////////////////////////////////
		// Writes SN76496 Register
		void WriteRegister(byte in_address, byte in_data)
		{
			byte register_index;

			// determine register value
			if ((in_data & 0x80) != 0)
			{
				register_index = RegisterIndex = (byte)((in_data >> 4) & 0x07);
				Registers[register_index] = (byte)((Registers[register_index] & 0x3f0) | (in_data & 0xf));
			}
			else
			{
				register_index = RegisterIndex;
				Registers[register_index] = (byte)((Registers[register_index] & 0x00f) | ((in_data & 0x3f) << 4));
			}

			// update register
			switch (register_index)
			{
				case 0: // tone 0: frequency
				case 2: // tone 1: frequency
				case 4: // tone 2: frequency
								// check for zero frequency
					Frequency[register_index / 2] = Registers[register_index];
					break;

				case 1: // tone 0: attenuation
				case 3: // tone 1: attenuation
				case 5: // tone 2: attenuation
				case 7: // Noise attenuation 
					Amplitude[register_index / 2] = l_amplitude_table[in_data & 0x0f];
					break;

				case 6: // Noise control register
					NoiseControl = (byte)(in_data & 0x07);          // set noise register
					NoiseShiftRegister = NOISE_INITIAL_STATE;       // reset shift register
					NoiseOutput = (sbyte)(NoiseShiftRegister & 1);  // set output
					break;
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// Sets panning for stereo output
		void SetPanning(byte in_channel, sbyte in_panning)
		{
			Paning[in_channel] = in_panning;
		}

		///////////////////////////////////////////////////////////////////////////////
		// Renders audio stream
		public void RenderAudioStream(ref int out_left, ref int out_right)
		{
			ushort sample_weight;
			ushort clock_cycles;
			ushort clock;
			ushort noise_divisor;
			byte noise_register_shift_count = 0;
			byte i;

			// update ClockCounter
			ClockCounter += ClockFrequency / CLOCK_DIVISOR;
			clock_cycles = (ushort)(ClockCounter / m_sample_rate);
			ClockCounter -= (uint)(clock_cycles * m_sample_rate);

			// handle channels 0,1,2
			for (i = 0; i < 3; i++)
			{
				if (Frequency[i] == 0)
				{
					Output[i] = 1;
				}
				else
				{
					if (Counter[i] < clock_cycles)
					{
						// run clock_cycle number of sound generarion cycle
						clock = clock_cycles;
						while (Counter[i] < clock)
						{
							// update output
							Output[i] = (sbyte)(-Output[i]);

							// output for noise register
							if (i == 3)
								noise_register_shift_count++;

							// update counter
							clock -= Counter[i];
							Counter[i] = Frequency[i];
						}
						Counter[i] -= clock;
					}
					else
					{
						Counter[i] -= (ushort)clock_cycles;
					}
				}

				// calculate sample
				m_sample[i] = (short)(Output[i] * Amplitude[i]);
			}

			// handle noise divisor (counter)
			if ((NoiseControl & 3) != 3)
			{
				noise_register_shift_count = 0;

				if (NoiseCounter < clock_cycles)
				{
					switch (NoiseControl & 3)
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

					NoiseCounter = (ushort)(noise_divisor / CLOCK_DIVISOR + NoiseCounter - clock_cycles);
					noise_register_shift_count = 1;
				}
				else
				{
					NoiseCounter -= clock_cycles;
				}
			}

			// handle noise register
			while (noise_register_shift_count > 0)
			{
				// update shift register
				NoiseShiftRegister = (ushort)((NoiseShiftRegister >> 1) | ((((NoiseControl & 4) != 0) ? CalculateParity(NoiseShiftRegister & NoiseTap) : NoiseShiftRegister & 1) << 15));

				noise_register_shift_count--;
			}

			// update output
			NoiseOutput = (sbyte)(((NoiseShiftRegister & 1) == 0) ? -1 : 1);

			// add noise output to the sample
			m_sample[3] = (short)(NoiseOutput * Amplitude[3]);

			// generate sample output
			if (m_stereo_mode)
			{
				// stereo output

				// left channel
				int sample_sum = 0;
				for (i = 0; i < 4; i++)
				{
					sample_weight = (ushort)(127 - Paning[i]);
					if (sample_weight > 254)
						sample_weight = 254;

					sample_sum += m_sample[i] * sample_weight / 254;
				}
				out_left += sample_sum;

				// right channel
				sample_sum = 0;
				for (i = 0; i < 4; i++)
				{
					sample_weight = (ushort)(Paning[i] + 127);
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

		///////////////////////////////////////////////////////////////////////////////
		// Calculates parity of a ushort
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