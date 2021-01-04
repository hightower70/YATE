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
// Interface for Cartridge Emulation
///////////////////////////////////////////////////////////////////////////////
namespace YATECommon
{
	/// <summary>
	/// Cartridge Emulation Interface
	/// </summary>
	public interface ITVCCartridge
	{
		// Cartridge memory read/write
		byte MemoryRead(ushort in_address);									// cartridge memory read
		void MemoryWrite(ushort in_address, byte in_byte);	// cartridge memory write

		// Cartridge maintenance functions
		void Initialize(ITVComputer in_parent);							// initializes cartridge (cartridge installation)
		void Remove(ITVComputer in_parent);									// removes cartridge (cartridge removal)
		void Reset();																				// Computer reset
	}
}
