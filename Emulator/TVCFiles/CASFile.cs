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
// TVC CAS file format handler class
///////////////////////////////////////////////////////////////////////////////
using System.IO;
using System.Runtime.InteropServices;
using YATECommon.Files;
using YATECommon.Helpers;

namespace YATE.Emulator.TVCFiles
{
	public class CASFile
	{
		///////////////////////////////////////////////////////////////////////////////
		// Loads CAS file
		public static void CASLoad(Stream in_file, ProgramStorage in_storage)
		{
      TVCFileTypes.CASUPMHeaderType upm_header = new TVCFileTypes.CASUPMHeaderType();
      TVCFileTypes.CASProgramFileHeaderType program_header = new TVCFileTypes.CASProgramFileHeaderType();

			// open CAS file
			using (BinaryReader cas_file = new BinaryReader(in_file))
			{
				// load UPM header
				cas_file.Read(upm_header);

				// load program header
				cas_file.Read(program_header);

				// Check validity
				if (!CASCheckHeaderValidity(program_header))
					throw new FileFormatException("Invalid CAS header");

				if (!CASCheckUPMHeaderValidity(upm_header))
					throw new FileFormatException("Invalid UPM header");

				cas_file.Read(in_storage.Data, 0, program_header.FileLength);

				in_storage.Length = program_header.FileLength;
				in_storage.CopyProtect = (upm_header.CopyProtect != 0);
				in_storage.AutoStart = (program_header.Autorun != 0);
				in_storage.SetFileTypeByte(program_header.FileType);
			}

		// generate TVC filename
		//PCToTVCFilename(g_db_file_name, in_file_name);

		}

		///////////////////////////////////////////////////////////////////////////////
		// Saves CAS file
		public static void CASSave(Stream in_file, ProgramStorage in_storage)
		{
			using (BinaryWriter cas_file = new BinaryWriter(in_file))
			{
        TVCFileTypes.CASUPMHeaderType upm_header = CreateUPMHeader(in_storage);
        TVCFileTypes.CASProgramFileHeaderType program_header = CreateProgramFileHeader(in_storage);

				cas_file.Write(upm_header);
				cas_file.Write(program_header);
				cas_file.Write(in_storage.Data, 0, in_storage.Length);
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// Checks UPM header validity
		public static bool CASCheckUPMHeaderValidity(TVCFileTypes.CASUPMHeaderType in_header)
		{
			return (in_header.FileType == TVCFileTypes.CASBlockHeaderFileUnbuffered);
		}

		///////////////////////////////////////////////////////////////////////////////
		// Cheks CAS header validity
		public static bool CASCheckHeaderValidity(TVCFileTypes.CASProgramFileHeaderType in_header)
		{
			return (in_header.Zero == 0 && (in_header.FileType == 0x00 || in_header.FileType ==0x01));
		}

		///////////////////////////////////////////////////////////////////////////////
		// Initialize CAS Headers
		public static TVCFileTypes.CASProgramFileHeaderType CreateProgramFileHeader(ProgramStorage in_storage)
		{
      TVCFileTypes.CASProgramFileHeaderType header = new TVCFileTypes.CASProgramFileHeaderType();

			header.FileType = in_storage.GetFileTypeByte();
			header.FileLength = in_storage.Length;
			header.Autorun = (byte)((in_storage.AutoStart) ? 0xff : 0x00);
			header.Version = 0;

			return header;
		}

		///////////////////////////////////////////////////////////////////////////////
		// Initizes UPM header
		public static TVCFileTypes.CASUPMHeaderType CreateUPMHeader(ProgramStorage in_storage)
		{
      TVCFileTypes.CASUPMHeaderType header = new TVCFileTypes.CASUPMHeaderType();

			ushort cas_length = (ushort)(in_storage.Length + Marshal.SizeOf(typeof(TVCFileTypes.CASUPMHeaderType)) + Marshal.SizeOf(typeof(TVCFileTypes.CASProgramFileHeaderType)));

			header.FileType = TVCFileTypes.CASBlockHeaderFileUnbuffered;
			header.CopyProtect = (byte)((in_storage.CopyProtect) ? 0xff : 0x00);
			header.BlockNumber = (ushort)(cas_length / 128);
			header.LastBlockBytes = (byte)(cas_length % 128);

			return header;
		}
	}
}
