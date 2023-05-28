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
// Storage class for storing TVC program files and related information
///////////////////////////////////////////////////////////////////////////////

namespace YATE.Emulator.Files
{
  public class ProgramStorage
	{
		public int StorageSize = 65536;

		public enum ProgramType
		{
			ASCII,
			Program
		}

		public byte[] Data { get; private set; }
		public string Name;
		public ushort Length { get; set; }
		public bool CopyProtect { get; set; }
		public bool AutoStart { get; set; }
		public ProgramType FileType { get; set; }

		public ProgramStorage()
		{
			Data = new byte[StorageSize];
			Name = "";
			Length = 0;
			CopyProtect = false;
			AutoStart = false;
			FileType = ProgramType.Program;
		}

		public byte GetFileTypeByte()
		{
			switch (FileType)
			{
				case ProgramType.ASCII:
					return 0;

				case ProgramType.Program:
					return 1;

				default:
					return 0xff;
			}
		}

		public void SetFileTypeByte(byte in_file_type)
		{
			switch (in_file_type)
			{
				case 0x00:
					FileType = ProgramType.ASCII;
					break;

				case 0x01:
					FileType = ProgramType.Program;
					break;

				default:
					break;
			}
		}
	}
}
