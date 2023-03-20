using System.Collections.Generic;
using YATE.Emulator.Z80CPU;

namespace YATE.Controls
{
  public class DisassemblyLineCollection : List<DisassemblyLine>
  {
    private int m_memory_offset;
    private byte[] m_memory;

    public DisassemblyLineCollection()
    {
    }

    public void Disassembly(byte[] in_memory, int in_memory_offset, ushort in_start_address, ushort in_last_address)
    {
      m_memory = in_memory;
      m_memory_offset = in_memory_offset;

      Z80Disassembler disassembler = new Z80Disassembler();
      disassembler.ReadByte = ReadMemoryByte;
      disassembler.HexConstDisplayMode = Z80Disassembler.HexConstDisplay.Postfix;

      Clear();

      ushort address = in_start_address;
      while (address < in_last_address)
      {
        Z80DisassemblerInstruction current_instruction = disassembler.Disassemble(address);
        address += current_instruction.Length;

        Add(new DisassemblyLine(current_instruction));
      }
    }


    private byte ReadMemoryByte(ushort in_address)
    {
      return m_memory[in_address + m_memory_offset];
    }

    public bool IsReadOnly
    {
      get
      {
        return true;
      }
    }

  }
}
