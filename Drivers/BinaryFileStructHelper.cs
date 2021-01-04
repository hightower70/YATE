///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019 Laszlo Arvai. All rights reserved.
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
// Helper class for reading writing c# object into, from binary file
///////////////////////////////////////////////////////////////////////////////
using System.IO;
using System.Runtime.InteropServices;

namespace YATE.Drivers
{
  public static class BinaryFileStructHelper
	{
		public static void Read(this BinaryReader in_binary_reader, object in_object)
		{
			//Read byte array
			byte[] buff = in_binary_reader.ReadBytes(Marshal.SizeOf(in_object));

			//Make sure that the Garbage Collector doesn't move our buffer 
			GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);
			//Marshal the bytes
			Marshal.PtrToStructure(handle.AddrOfPinnedObject(), in_object);
			handle.Free();//Give control of the buffer back to the GC 
		}

		public static void Write(this BinaryWriter in_binary_writer, object in_object)
		{ 
			byte[] buff = new byte[Marshal.SizeOf(in_object)];					//Create Buffer
			GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);//Hands off GC
																																	//Marshal the structure
			Marshal.StructureToPtr(in_object, handle.AddrOfPinnedObject(), false);
			handle.Free();

			in_binary_writer.Write(buff);
		}
	}
}
