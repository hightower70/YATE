using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVCEmu.Helpers
{
	public class CRC16
	{
		readonly ushort[] table = new ushort[256];
		private ushort m_crc;
		private ushort m_crc_init;

		public CRC16(ushort in_polynomial, ushort in_init)
		{
			ushort temp, value;

			m_crc_init = in_init;

			for (int i = 0; i < table.Length; i++)
			{
				temp = 0;
				value = (ushort)(i << 8);
				for (int j = 0; j < 8; j++)
				{
					if (((temp ^ value) & 0x8000) != 0)
					{
						temp = (ushort)((temp << 1) ^ in_polynomial);
					}
					else
					{
						temp <<= 1;
					}
					value <<= 1;
				}
				table[i] = temp;
			}
		}

		public ushort CRC
		{
			get { return m_crc; }
		}

		public byte CRCLow
		{
			get { return (byte)(m_crc & 0xff); }
		}

		public byte CRCHigh
		{
			get { return (byte)(m_crc >> 8); }
		}

		public void Reset()
		{
			m_crc = m_crc_init;
		}

		public void Add(byte in_byte)
		{
			m_crc = (ushort)((m_crc << 8) ^ table[(m_crc >> 8) ^ (0xff & in_byte)]);
		}

		public void Add(byte[] in_bytes)
		{
			for (int i = 0; i < in_bytes.Length; ++i)
			{
				m_crc = (ushort)((m_crc << 8) ^ table[(m_crc >> 8) ^ (0xff & in_bytes[i])]);
			}
		}

		public void Add(byte[] in_bytes, int in_length)
		{
			for (int i = 0; i < in_length; ++i)
			{
				m_crc = (ushort)((m_crc << 8) ^ table[(m_crc >> 8) ^ (0xff & in_bytes[i])]);
			}
		}
	}
}

