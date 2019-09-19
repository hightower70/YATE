using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TVCEmu.Models.TVCFiles
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
