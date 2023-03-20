///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2023 Laszlo Arvai. All rights reserved.
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
// CITT16 CRC Calculation Class
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YATECommon.Helpers
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

