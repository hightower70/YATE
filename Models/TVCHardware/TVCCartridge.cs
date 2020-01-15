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
// Videoton TV Computer ROM Cartridge Emulation
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVCEmuCommon;

namespace TVCEmu.Models.TVCHardware
{
	class TVCCartridge : ITVCCartridge
	{
		public const int CartMemLength = 0x4000;

		public byte[] Memory { get; private set; }

		public byte MemoryRead(ushort in_address)
		{
			return Memory[in_address];
		}

		public void MemoryWrite(ushort in_address, byte in_byte)
		{
			// no write
		}

		public void Reset()
		{
			// no reset
		}

		public void Initialize(ITVComputer in_parent)
		{
			Memory = new byte[CartMemLength];

			for (int i = 0; i < Memory.Length; i++)
				Memory[i] = 0xff;
		}

		public void Remove()
		{
			// no remove
		}
	}
}
