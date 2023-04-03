using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YATE.Controls;
using YATE.Emulator.Z80CPU;
using YATECommon;

namespace YATE.Disassembler
{
  public class MemoryDisassembler
  {
    private const int BytesFieldLength = 13;
    private const int MnemonicWidth = 4;

    private IDebuggableMemory m_memory;


    public ushort MemoryStartAddress { get; set; } = 0x0000;
    public ushort MemoryEndAddress { get; set; } = 0xffff;

    public TVCMemoryType MemoryType { get; set; } = TVCMemoryType.RAM;

    public int MemoryPageIndex { get; set; } = 0;


    public List<DisassemblyLine> Disassemble()
    {
      List<DisassemblyLine> disassembly_lines = new List<DisassemblyLine>();
      DisassemblyLine disassembly_line;

      // get memory
      m_memory = TVCManagers.Default.DebugManager.GetDebuggableMemory(MemoryType);
      if (m_memory == null)
        return disassembly_lines;

      // prepare disassembler
      Z80Disassembler disassembler = new Z80Disassembler();
      disassembler.ReadByte = ReadMemoryByte;
      disassembler.HexConstDisplayMode = Z80Disassembler.HexConstDisplay.Postfix;

      int address = MemoryStartAddress;
      int end_address = MemoryEndAddress;

      if (address < m_memory.AddressOffset)
        address = m_memory.AddressOffset;

      if (end_address < address)
        end_address = address;
      if (end_address > m_memory.AddressOffset + m_memory.MemorySize)
        end_address = m_memory.AddressOffset + m_memory.MemorySize;

      disassembler.AddressOffset = m_memory.AddressOffset;

      // disassembly memory
      while (address < end_address)
      {
        Z80DisassemblerInstruction current_instruction = disassembler.Disassemble((ushort)address);
        disassembly_line = new DisassemblyLine(current_instruction);
        disassembly_line.Index = disassembly_lines.Count;
        disassembly_line.DisassemblyInstruction.Mnemonic = disassembly_line.DisassemblyInstruction.Mnemonic.PadRight(MnemonicWidth);
        disassembly_line.DisassemblyInstruction.Bytes = disassembly_line.DisassemblyInstruction.Bytes.PadRight(BytesFieldLength);

        disassembly_lines.Add(disassembly_line);
        address += current_instruction.Length;
      }

      return disassembly_lines;
    }


    private byte ReadMemoryByte(ushort in_address)
    {
      return m_memory.DebugReadMemory(MemoryPageIndex, in_address);
    }
  }
}
