///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2020 Laszlo Arvai. All rights reserved.
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
// TVC style CRC calculation used CAS files
///////////////////////////////////////////////////////////////////////////////

namespace YATE.Emulator.Files
{
  class TVCFileCRC
	{
    /// <summary>
    /// Adds one bit for the CRC
    /// </summary>
    /// <param name="in_bit">Bit value to add</param>
    /// <returns>Current value of the CRC</returns>
		private ushort CRCAddBit(bool in_bit)
		{
			byte A;
			byte CY;

			if (in_bit != false)							//     LD  A,80
				A = 0x80;												//     JR  NZ,L1
			else															//
				A = 0;													//     XOR A
																				//
			A = (byte)(A ^ (CRC >> 8));				// L1: XOR H
			CY = (byte)(A & 0x80);						//     RLA
																				//
			if (CY != 0)											//     JR  NC,L2
			{																	//     LD  A,H
				CRC ^= 0x0810;									//     XOR 08
				CY = 1;													//     LD  H,A
																				//     LD  A,L
																				//     XOR 10
																				//     LD  L,A
			}																	//     SCF
																				//
			CRC += (ushort)(CRC + CY);				//     ADC HL,HL

			return CRC;
		}

		///////////////////////////////////////////////////////////////////////////////
		// Initializes CRC value
		private void CRCReset()
		{
			CRC = 0;
		}

    /// <summary>
    /// Gets current value of the CRC
    /// </summary>
		public ushort CRC { get; private set; }

    /// <summary>
    /// Adds CRC of one byte to the current value of the CRC
    /// </summary>
    /// <param name="in_data"></param>
    /// <returns></returns>
		public ushort CRCAddByte(byte in_data)
		{
			int i;

			for (i = 0; i < 8; i++)
			{
				if ((in_data & 0x01) == 0)
				{
					CRCAddBit(false);
				}
				else
				{
					CRCAddBit(true);
				}

				in_data >>= 1;
			}

			return CRC;
		}

		/// <summary>
    /// Add CRC of a block to the current value of the CRC
    /// </summary>
    /// <param name="in_buffer">Buffer to add</param>
    /// <param name="in_buffer_length">Number of bytes in the buffer to add</param>
    /// <returns>Current value of the CRC</returns>
		public ushort CRCAddBlock(byte[] in_buffer, int in_buffer_length)
		{
			int i = 0;
			while (in_buffer_length > 0)
			{
				CRCAddByte(in_buffer[i++]);
				in_buffer_length--;
			}

			return CRC;
		}
	}
}
