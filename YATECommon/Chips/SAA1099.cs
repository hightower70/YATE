using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace YATECommon.Chips
{
  /***************************************************************************

			Philips SAA1099 Sound driver

			By Juergen Buchmueller and Manuel Abadia

			SAA1099 register layout:
			========================

			offs | 7654 3210 | description
			-----+-----------+---------------------------
			0x00 | ---- xxxx | Amplitude channel 0 (left)
			0x00 | xxxx ---- | Amplitude channel 0 (right)
			0x01 | ---- xxxx | Amplitude channel 1 (left)
			0x01 | xxxx ---- | Amplitude channel 1 (right)
			0x02 | ---- xxxx | Amplitude channel 2 (left)
			0x02 | xxxx ---- | Amplitude channel 2 (right)
			0x03 | ---- xxxx | Amplitude channel 3 (left)
			0x03 | xxxx ---- | Amplitude channel 3 (right)
			0x04 | ---- xxxx | Amplitude channel 4 (left)
			0x04 | xxxx ---- | Amplitude channel 4 (right)
			0x05 | ---- xxxx | Amplitude channel 5 (left)
			0x05 | xxxx ---- | Amplitude channel 5 (right)
					 |           |
			0x08 | xxxx xxxx | Frequency channel 0
			0x09 | xxxx xxxx | Frequency channel 1
			0x0a | xxxx xxxx | Frequency channel 2
			0x0b | xxxx xxxx | Frequency channel 3
			0x0c | xxxx xxxx | Frequency channel 4
			0x0d | xxxx xxxx | Frequency channel 5
					 |           |
			0x10 | ---- -xxx | Channel 0 octave select
			0x10 | -xxx ---- | Channel 1 octave select
			0x11 | ---- -xxx | Channel 2 octave select
			0x11 | -xxx ---- | Channel 3 octave select
			0x12 | ---- -xxx | Channel 4 octave select
			0x12 | -xxx ---- | Channel 5 octave select
					 |           |
			0x14 | ---- ---x | Channel 0 frequency enable (0 = off, 1 = on)
			0x14 | ---- --x- | Channel 1 frequency enable (0 = off, 1 = on)
			0x14 | ---- -x-- | Channel 2 frequency enable (0 = off, 1 = on)
			0x14 | ---- x--- | Channel 3 frequency enable (0 = off, 1 = on)
			0x14 | ---x ---- | Channel 4 frequency enable (0 = off, 1 = on)
			0x14 | --x- ---- | Channel 5 frequency enable (0 = off, 1 = on)
					 |           |
			0x15 | ---- ---x | Channel 0 noise enable (0 = off, 1 = on)
			0x15 | ---- --x- | Channel 1 noise enable (0 = off, 1 = on)
			0x15 | ---- -x-- | Channel 2 noise enable (0 = off, 1 = on)
			0x15 | ---- x--- | Channel 3 noise enable (0 = off, 1 = on)
			0x15 | ---x ---- | Channel 4 noise enable (0 = off, 1 = on)
			0x15 | --x- ---- | Channel 5 noise enable (0 = off, 1 = on)
					 |           |
			0x16 | ---- --xx | Noise generator parameters 0
			0x16 | --xx ---- | Noise generator parameters 1
					 |           |
			0x18 | --xx xxxx | Envelope generator 0 parameters
			0x18 | x--- ---- | Envelope generator 0 control enable (0 = off, 1 = on)
			0x19 | --xx xxxx | Envelope generator 1 parameters
			0x19 | x--- ---- | Envelope generator 1 control enable (0 = off, 1 = on)
					 |           |
			0x1c | ---- ---x | All channels enable (0 = off, 1 = on)
			0x1c | ---- --x- | Synch & Reset generators

	***************************************************************************/
  public class SAA1099
  {
    #region · Types ·

    /* this structure defines a channel */
    class SAA1099Channel
    {
      public int Frequency;                 /* frequency (0x00..0xff) */
      public int FreqEnable;                /* frequency enable */
      public int NoiseEnable;               /* noise enable */
      public int Octave;                    /* octave (0x00..0x07) */
      public int[] Amplitude = new int[2];  /* amplitude (0x00..0x0f) */
      public int[] Envelope = new int[2];   /* envelope (0x00..0x0f or 0x10 == off) */

      /* vars to simulate the square wave */
      public double Counter;
      public double Freq;
      public int Level;
    };

    /* this structure defines a noise channel */
    class SAA1099Noise
    {
      /* vars to simulate the noise generator output */
      public double Counter;
      public double Freq;
      public int Level;            /* noise polynomal shifter */
    };


    #region · Constant ·

    private const int LEFT = 0x00;
    private const int RIGHT = 0x01;

    static readonly int[] amplitude_lookup = {
       0*32767/16,  1*32767/16,  2*32767/16,  3*32767/16,
       4*32767/16,  5*32767/16,  6*32767/16,  7*32767/16,
       8*32767/16,  9*32767/16, 10*32767/16, 11*32767/16,
      12*32767/16, 13*32767/16, 14*32767/16, 15*32767/16
    };

    static readonly byte[][] envelope = {
	    /* zero amplitude */
	    new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },

	    /* maximum amplitude */
       new byte[] {15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,
       15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,
       15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,
         15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15, },

      /* single decay */
	    new byte[] {15,14,13,12,11,10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },

	    /* repetitive decay */
	    new byte[] {15,14,13,12,11,10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
       15,14,13,12,11,10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
       15,14,13,12,11,10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
       15,14,13,12,11,10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 },
	    /* single triangular */

	    new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,
       15,14,13,12,11,10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
	  
      /* repetitive triangular */
	    new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,
       15,14,13,12,11,10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,
       15,14,13,12,11,10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 },

      /* single attack */
      new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,
      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
	  
      /* repetitive attack */
      new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,
      0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,
      0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15 }
    };

    #endregion

    #endregion

    #region · Data members ·
    private int[] m_noise_params = new int[2];      /* noise generators parameters */
    private int[] m_env_enable = new int[2];        /* envelope generators enable */
    private int[] m_env_reverse_right = new int[2];   /* envelope reversed for right channel */
    private int[] m_env_mode = new int[2];        /* envelope generators mode */
    private int[] m_env_bits = new int[2];        /* non zero = 3 bits resolution */
    private int[] m_env_clock = new int[2];       /* envelope clock mode (non-zero external) */
    private int[] m_env_step = new int[2];                /* current envelope step */
    private int m_all_ch_enable;        /* all channels enable */
    private int m_sync_state;         /* sync all channels */
    private int m_selected_reg;       /* selected register */
    private SAA1099Channel[] m_channels;    /* channels */
    private SAA1099Noise[] m_noise; /* noise generators */
    private double m_sample_rate;

    #endregion

    /// <summary>
    /// Default constructor
    /// </summary>
    public SAA1099()
    {
      m_channels = new SAA1099Channel[6];
      for (int i = 0; i < m_channels.Length; i++)
        m_channels[i] = new SAA1099Channel();

      m_noise = new SAA1099Noise[2];
      for (int i = 0; i < m_noise.Length; i++)
        m_noise[i] = new SAA1099Noise();
    }

    /// <summary>
    /// Initializes SAA1099 chip with the specified audio sample rate
    /// </summary>
    /// <param name="in_sample_rate">Desired sample rate for audio rendering</param>
    public void Initialize(uint in_sample_rate)
    {
      m_sample_rate = in_sample_rate;
    }

    /// <summary>
    /// Writes address register
    /// </summary>
    /// <param name="in_data"></param>
    public void WriteAddressRegister(byte in_data)
    {
      if ((in_data & 0xff) > 0x1c)
      {
        /* Error! */
        Debug.WriteLine("SAA1099: Unknown register selected");
      }

      m_selected_reg = in_data & 0x1f;
      if (m_selected_reg == 0x18 || m_selected_reg == 0x19)
      {
        /* clock the envelope channels */
        if (m_env_clock[0] != 0)
          saa1099_envelope(0);
        if (m_env_clock[1] != 0)
          saa1099_envelope(1);
      }
    }

    /// <summary>
    /// Writes control register
    /// </summary>
    /// <param name="in_data"></param>
    public void WriteControlRegister(byte in_data)
    {
      int ch;

      /* first update the stream to this point in time */
      //stream_update(saa->stream);

      switch (m_selected_reg)
      {
        /* channel i amplitude */
        case 0x00:
        case 0x01:
        case 0x02:
        case 0x03:
        case 0x04:
        case 0x05:
          ch = m_selected_reg & 7;
          m_channels[ch].Amplitude[LEFT] = amplitude_lookup[in_data & 0x0f];
          m_channels[ch].Amplitude[RIGHT] = amplitude_lookup[(in_data >> 4) & 0x0f];
          break;

        /* channel i frequency */
        case 0x08:
        case 0x09:
        case 0x0a:
        case 0x0b:
        case 0x0c:
        case 0x0d:
          ch = m_selected_reg & 7;
          m_channels[ch].Frequency = in_data & 0xff;
          break;

        /* channel i octave */
        case 0x10:
        case 0x11:
        case 0x12:
          ch = (m_selected_reg - 0x10) << 1;
          m_channels[ch + 0].Octave = in_data & 0x07;
          m_channels[ch + 1].Octave = (in_data >> 4) & 0x07;
          break;

        /* channel i frequency enable */
        case 0x14:
          m_channels[0].FreqEnable = in_data & 0x01;
          m_channels[1].FreqEnable = in_data & 0x02;
          m_channels[2].FreqEnable = in_data & 0x04;
          m_channels[3].FreqEnable = in_data & 0x08;
          m_channels[4].FreqEnable = in_data & 0x10;
          m_channels[5].FreqEnable = in_data & 0x20;
          break;

        /* channel i noise enable */
        case 0x15:
          m_channels[0].NoiseEnable = in_data & 0x01;
          m_channels[1].NoiseEnable = in_data & 0x02;
          m_channels[2].NoiseEnable = in_data & 0x04;
          m_channels[3].NoiseEnable = in_data & 0x08;
          m_channels[4].NoiseEnable = in_data & 0x10;
          m_channels[5].NoiseEnable = in_data & 0x20;
          break;

        /* noise generators parameters */
        case 0x16:
          m_noise_params[0] = in_data & 0x03;
          m_noise_params[1] = (in_data >> 4) & 0x03;
          break;

        /* envelope generators parameters */
        case 0x18:
        case 0x19:
          ch = m_selected_reg - 0x18;
          m_env_reverse_right[ch] = in_data & 0x01;
          m_env_mode[ch] = (in_data >> 1) & 0x07;
          m_env_bits[ch] = in_data & 0x10;
          m_env_clock[ch] = in_data & 0x20;
          m_env_enable[ch] = in_data & 0x80;
          /* reset the envelope */
          m_env_step[ch] = 0;
          break;

        /* channels enable & reset generators */
        case 0x1c:
          m_all_ch_enable = in_data & 0x01;
          m_sync_state = in_data & 0x02;
          if ((in_data & 0x02) != 0)
          {
            int i;

            /* Synch & Reset generators */
            for (i = 0; i < 6; i++)
            {
              m_channels[i].Level = 0;
              m_channels[i].Counter = 0.0;
            }
          }
          break;

        default:  /* Error! */
          break;
      }
    }

    /// <summary>
    /// Renders audio stream
    /// </summary>
    /// <param name="out_left">Left channel output data</param>
    /// <param name="out_right">Right channel output data</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderAudioStream(ref int out_left, ref int out_right)
    {
      int ch;

      /* if the channels are disabled we're done */
      if (m_all_ch_enable == 0)
      {
        return;
      }

      for (ch = 0; ch < 2; ch++)
      {
        switch (m_noise_params[ch])
        {
          case 0: m_noise[ch].Freq = 31250.0 * 2; break;
          case 1: m_noise[ch].Freq = 15625.0 * 2; break;
          case 2: m_noise[ch].Freq = 7812.5 * 2; break;
          case 3: m_noise[ch].Freq = m_channels[ch * 3].Freq; break;
        }
      }

      /* fill all data needed */
      int output_l = 0, output_r = 0;

      /* for each channel */
      for (ch = 0; ch < 6; ch++)
      {
        if (m_channels[ch].Freq == 0.0)
          m_channels[ch].Freq = (double)((2 * 15625) << m_channels[ch].Octave) / (511.0 - (double)m_channels[ch].Frequency);

        /* check the actual position in the square wave */
        m_channels[ch].Counter -= m_channels[ch].Freq;
        while (m_channels[ch].Counter < 0)
        {
          /* calculate new frequency now after the half wave is updated */
          m_channels[ch].Freq = (double)((2 * 15625) << m_channels[ch].Octave) / (511.0 - (double)m_channels[ch].Frequency);

          m_channels[ch].Counter += m_sample_rate;
          m_channels[ch].Level ^= 1;

          /* eventually clock the envelope counters */
          if (ch == 1 && m_env_clock[0] == 0)
            saa1099_envelope(0);
          if (ch == 4 && m_env_clock[1] == 0)
            saa1099_envelope(1);
        }


        /* if the noise is enabled */
        if (m_channels[ch].NoiseEnable != 0)
        {
          /* if the noise level is high (noise 0: chan 0-2, noise 1: chan 3-5) */
          if ((m_noise[ch / 3].Level & 1) != 0)
          {
            /* subtract to avoid overflows, also use only half amplitude */
            output_l -= m_channels[ch].Amplitude[LEFT] * m_channels[ch].Envelope[LEFT] / 16 / 2;
            output_r -= m_channels[ch].Amplitude[RIGHT] * m_channels[ch].Envelope[RIGHT] / 16 / 2;
          }
        }

        /* if the square wave is enabled */
        if (m_channels[ch].FreqEnable != 0)
        {
          /* if the channel level is high */
          if ((m_channels[ch].Level & 1) != 0)
          {
            output_l += m_channels[ch].Amplitude[LEFT] * m_channels[ch].Envelope[LEFT] / 16;
            output_r += m_channels[ch].Amplitude[RIGHT] * m_channels[ch].Envelope[RIGHT] / 16;
          }
        }
      }

      for (ch = 0; ch < 2; ch++)
      {
        /* check the actual position in noise generator */
        m_noise[ch].Counter -= m_noise[ch].Freq;
        while (m_noise[ch].Counter < 0)
        {
          m_noise[ch].Counter += m_sample_rate;
          if (((m_noise[ch].Level & 0x4000) == 0) == ((m_noise[ch].Level & 0x0040) == 0))
            m_noise[ch].Level = (m_noise[ch].Level << 1) | 1;
          else
            m_noise[ch].Level <<= 1;
        }
      }

      /* write sound data to the buffer */
      out_left += output_l / 6;
      out_right += output_r / 6;
    }

    #region · Private members ·

    private void saa1099_envelope(int ch)
    {
      if (m_env_enable[ch] != 0)
      {
        int step, mode, mask;
        mode = m_env_mode[ch];
        /* step from 0..63 and then loop in steps 32..63 */
        step = m_env_step[ch] = ((m_env_step[ch] + 1) & 0x3f) | (m_env_step[ch] & 0x20);

        mask = 15;
        if (m_env_bits[ch] != 0)
          mask &= ~1;   /* 3 bit resolution, mask LSB */

        m_channels[ch * 3 + 0].Envelope[LEFT] =
        m_channels[ch * 3 + 1].Envelope[LEFT] =
        m_channels[ch * 3 + 2].Envelope[LEFT] = envelope[mode][step] & mask;

        if ((m_env_reverse_right[ch] & 0x01) != 0)
        {
          m_channels[ch * 3 + 0].Envelope[RIGHT] =
          m_channels[ch * 3 + 1].Envelope[RIGHT] =
          m_channels[ch * 3 + 2].Envelope[RIGHT] = (15 - envelope[mode][step]) & mask;
        }
        else
        {
          m_channels[ch * 3 + 0].Envelope[RIGHT] =
          m_channels[ch * 3 + 1].Envelope[RIGHT] =
          m_channels[ch * 3 + 2].Envelope[RIGHT] = envelope[mode][step] & mask;
        }
      }
      else
      {
        /* envelope mode off, set all envelope factors to 16 */
        m_channels[ch * 3 + 0].Envelope[LEFT] =
        m_channels[ch * 3 + 1].Envelope[LEFT] =
        m_channels[ch * 3 + 2].Envelope[LEFT] =
        m_channels[ch * 3 + 0].Envelope[RIGHT] =
        m_channels[ch * 3 + 1].Envelope[RIGHT] =
        m_channels[ch * 3 + 2].Envelope[RIGHT] = 16;
      }
    }
    #endregion

  }
}

