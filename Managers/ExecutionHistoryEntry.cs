using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YATE.Managers
{
	public class ExecutionHistoryEntry
	{
		public const int HistoryEntryByteBufferLength = 6;

		public ushort PC { get; set; }
		public uint TCycle { get; set; }
		public byte[] Bytes { get; private set; }

		public ExecutionHistoryEntry()
		{
			Bytes = new byte[HistoryEntryByteBufferLength];
		}

		public byte ReadMemory(ushort in_address)
		{
			int address = in_address - PC;

			if (address < 0 || address >= Bytes.Length)
				return 0;
			else
				return Bytes[address];
		}

		public void CopyTo(ExecutionHistoryEntry in_entry)
		{
			in_entry.PC = PC;
			in_entry.TCycle = TCycle;
			Bytes.CopyTo(in_entry.Bytes, 0);
		}
	}
}
