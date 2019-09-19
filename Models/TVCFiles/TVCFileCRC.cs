/*****************************************************************************/
/* TVCTape - Videoton TV Computer Tape Emulator                              */
/* CRC Calculation routines                                                  */
/*                                                                           */
/* Copyright (C) 2013 Laszlo Arvai                                           */
/* All rights reserved.                                                      */
/*                                                                           */
/* This software may be modified and distributed under the terms             */
/* of the BSD license.  See the LICENSE file for details.                    */
/*****************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TVCEmu.Models.TVCFiles
{
	class TVCFileCRC
	{
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

		///////////////////////////////////////////////////////////////////////////////
		// Gets current CRC value
		public ushort CRC { get; private set; }

		///////////////////////////////////////////////////////////////////////////////
		// Calculates CRC
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

		///////////////////////////////////////////////////////////////////////////////
		// Adds buffer content to the CRC
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
