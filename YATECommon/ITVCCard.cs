///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2018-2021 Laszlo Arvai. All rights reserved.
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
// Interface for emulated TVC card
///////////////////////////////////////////////////////////////////////////////
using YATECommon.Settings;

namespace YATECommon
{
	public interface ITVCCard
	{
		// memory read/write
		byte MemoryRead(ushort in_address);
		void MemoryWrite(ushort in_address, byte in_byte);

		// port read/write
		void PortRead(ushort in_address, ref byte inout_data);
		void PortWrite(ushort in_address, byte in_byte);

    // emulation functions
		void PeriodicCallback(ulong in_cpu_tick);
    void Reset();
    byte GetID();

    // card management
    //int SlotIndex { get; }
    void Install(ITVComputer in_parent);
    void Remove(ITVComputer in_parent);
  }
}
