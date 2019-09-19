using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVCEmu.Models.TVCFiles
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
