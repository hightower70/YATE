﻿///////////////////////////////////////////////////////////////////////////////
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
// Handles (save, load) TVC related program files (DSK, CAS from FAT12, UPM, ZIP image)
///////////////////////////////////////////////////////////////////////////////
using System.IO;
using System.IO.Compression;
using YATE.Emulator.TVCHardware;
using YATE.Emulator.Files.FAT12;

namespace YATE.Emulator.Files
{
  class TVCFiles
	{
		public static void LoadProgramFile(string in_file_name, TVCMemory in_memory)
		{
			ProgramStorage storage = new ProgramStorage();

			string extension = Path.GetExtension(in_file_name);

			if (string.Compare(extension, ".zip", true) == 0)
			{
				using (ZipArchive archive = ZipFile.Open(in_file_name, ZipArchiveMode.Read))
				{
					foreach (ZipArchiveEntry entry in archive.Entries)
					{
						// Load CAS file
						if (string.Compare(Path.GetExtension(entry.Name), ".cas", true) == 0)
						{
							using (Stream file_stream = entry.Open())
							{
								LoadCAS(file_stream, storage);
							}
							break;
						}

						// Load DSK file
						if (string.Compare(Path.GetExtension(entry.Name), ".dsk", true) == 0)
						{
							using (FileStream file_stream = new FileStream(in_file_name, FileMode.Open, FileAccess.Read, FileShare.Read))
							{
								LoadDSK(file_stream, storage);
							}
							break;
						}

					}
				}
			}
			else
			{
				// Load CAS file
				if (string.Compare(extension, ".cas", true) == 0)
				{
					using (FileStream file_stream = new FileStream(in_file_name, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						LoadCAS(file_stream, storage);
					}
				}

				// Load DSK file
				if (string.Compare(extension, ".dsk", true) == 0)
				{
					using (FileStream file_stream = new FileStream(in_file_name, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						LoadDSK(file_stream, storage);
					}
				}
			}

			// Set memory content
			in_memory.LoadFromProgramStorage(storage);
		}

		public static void LoadDSK(Stream in_stream, ProgramStorage in_storage)
		{
			//Read File as FAT12 Image
			FatPartition FAT = new FatPartition(in_stream);

			foreach(FatDirectoryEntry entry in FAT.RootDirectory)
			{
				if(string.Compare(entry.Extension, "cas", true)==0)
				{
					byte[] cas_file = FAT.ReadFile(entry, in_stream);

					using (MemoryStream cas_stream = new MemoryStream(cas_file))
					{
						LoadCAS(cas_stream, in_storage);
					}
					break;
				}
			}
		}

		public static void LoadCAS(Stream in_stream, ProgramStorage in_storage)
		{
			CASFile.CASLoad(in_stream, in_storage);
		}

		public static void SaveCAS(Stream in_stream, ProgramStorage in_storage)
		{
			CASFile.CASSave(in_stream, in_storage);
		}

		public static void SaveProgramFile(string in_file_name, TVCMemory in_memory)
		{
			using (FileStream cas_stream = new FileStream(in_file_name, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096))
			{
				ProgramStorage storage = new ProgramStorage();

				// get memory content
				in_memory.SaveToProgramStorage(storage);

				SaveCAS(cas_stream, storage);
			}
		}

		public static void SaveProgramFile(string in_file_name, TVCMemory in_memory, BASFile.EncodingType in_encoding)
		{
			using (FileStream bas_stream = new FileStream(in_file_name, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096))
			{
				ProgramStorage storage = new ProgramStorage();

				// get memory content
				in_memory.SaveToProgramStorage(storage);

				BASFile bas_file = new BASFile();
				bas_file.Encoding = in_encoding;
				bas_file.Save(bas_stream, storage);
			}
		}
	}
}
