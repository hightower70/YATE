using System.Text;
using static YATE.Emulator.Z80CPU.Z80DisassemblerTable;

// Ported and modified from http://z80ex.sourceforge.net/


namespace YATE.Emulator.Z80CPU
{
  public class Z80Disassembler
  {
    public enum HexConstDisplay
    {
      None,
      Prefix,
      Postfix
    };

    public HexConstDisplay HexConstDisplayMode { get; set; } = HexConstDisplay.None;

    public delegate byte ReadByteDelegate(ushort in_address);

    public bool ShowRelativeOffsets { get; set; } = false;


    public int AddressOffset { get; set; } = 0;


    /// <summary>
    /// Delegate function for reading memory content
    /// </summary>
    public ReadByteDelegate ReadByte { get; set; }

    /// <summary>
    /// Disassembles one instruction
    /// </summary>
    /// <param name="in_address">Memory address for disassembly start</param>
    /// <returns>Disassembled instruction</returns>
    public Z80DisassemblerInstruction Disassemble(ushort in_address)
    {
      OpCode opcode = null;
      Z80DisassemblerInstruction dissassembled_instruction = new Z80DisassemblerInstruction();
      dissassembled_instruction.Address = in_address;
      dissassembled_instruction.NumericOperand = 0;

      ushort start_address = in_address;

      bool have_displacement = false;
      byte displacement = 0;

      ushort jump_address = 0;
      bool have_jump_address = false;

      have_displacement = false;
      displacement = 0;

      byte opc = DisassemblerReadByte(in_address++, dissassembled_instruction);

      switch (opc)
      {
        case 0xDD:
        case 0xFD:
          byte next = DisassemblerReadByte(in_address++, dissassembled_instruction);
          if ((next | 0x20) == 0xFD || next == 0xED)
          {
            dissassembled_instruction.Asm = "NOP*";
            dissassembled_instruction.TStates = 4;
            dissassembled_instruction.TStates2 = 0;
            opcode = invalid_opcode;
          }
          else if (next == 0xCB)
          {
            displacement = DisassemblerReadByte(in_address++, dissassembled_instruction);
            next = DisassemblerReadByte(in_address++, dissassembled_instruction);

            opcode = (opc == 0xDD) ? dasm_ddcb[next] : dasm_fdcb[next];
            have_displacement = true;
          }
          else
          {
            opcode = (opc == 0xDD) ? dasm_dd[next] : dasm_fd[next];
            if (opcode.Mnemonic == null) //mirrored instructions
            {
              opcode = dasm_base[next];
              dissassembled_instruction.TStates = 4;
              dissassembled_instruction.TStates2 = 4;
            }
          }
          break;

        case 0xED:
          next = DisassemblerReadByte(in_address++, dissassembled_instruction);
          opcode = dasm_ed[next];
          if (opcode.Mnemonic == null)
          {
            dissassembled_instruction.Asm = "NOP*";
            dissassembled_instruction.TStates = 8;
            opcode = invalid_opcode;
          }
          break;

        case 0xCB:
          next = DisassemblerReadByte(in_address++, dissassembled_instruction);
          opcode = dasm_cb[next];
          break;

        default:
          opcode = dasm_base[opc];
          break;
      }

      if (opcode != null)
      {
        dissassembled_instruction.OpCode = opcode;

        byte operand_index = 1;
        var sb = new StringBuilder();

        foreach (var ch in opcode.Mnemonic)
        {
          switch (ch)
          {

            case '@':
              {
                var lo = DisassemblerReadByte(in_address++, dissassembled_instruction);
                var hi = DisassemblerReadByte(in_address++, dissassembled_instruction);
                ushort val = (ushort)(lo + hi * 0x100);

                if ((opcode.Flags & (OpCodeFlags.RefAddr | OpCodeFlags.Jumps)) != 0)
                  sb.Append(FormatAddr(val));
                else
                  sb.Append(FormatWord(val));

                dissassembled_instruction.WordVal = val;

                jump_address = val;
                have_jump_address = true;

                dissassembled_instruction.NumericOperand = operand_index;
                break;
              }

            case '$':
            case '%':
              {
                if (!have_displacement)
                  displacement = DisassemblerReadByte(in_address++, dissassembled_instruction);

                var disp = (displacement & 0x80) != 0 ? -(((~displacement) & 0x7f) + 1) : displacement;

                if (ShowRelativeOffsets)
                {
                  if (disp > 0)
                  {
                    dissassembled_instruction.Comment = string.Format("+{0}", disp);
                  }
                  else
                  {
                    dissassembled_instruction.Comment = string.Format("{0}", disp);
                  }
                }

                if (ch == '$')
                {
                  if ((sbyte)disp < 0 && sb[sb.Length - 1] == '+')
                  {
                    sb.Length--;
                  }
                  sb.Append(FormatSignedByte((sbyte)disp));
                }
                else
                {
                  jump_address = (ushort)(in_address + disp);
                  have_jump_address = true;
                  sb.Append(FormatAddr(jump_address));
                }

                dissassembled_instruction.NumericOperand = operand_index;

                break;
              }

            case '#':
              {
                var lo = DisassemblerReadByte(in_address++, dissassembled_instruction);
                sb.Append(FormatByte(lo));
                if (lo >= 0x20 && lo <= 0x7f)
                  dissassembled_instruction.Comment = string.Format("'{0}'", (char)lo);
                dissassembled_instruction.ByteVal = lo;

                dissassembled_instruction.NumericOperand = operand_index;

                break;
              }

            default:
              if (ch == ',')
                operand_index++;

              sb.Append(ch);
              break;
          }

        }

        dissassembled_instruction.Asm = sb.ToString();

        dissassembled_instruction.TStates += opcode.TStates;
        dissassembled_instruction.TStates2 += opcode.TStates2;

        // Return continue address
        if ((opcode.Flags & OpCodeFlags.Continues) != 0)
          dissassembled_instruction.NextAddress1 = in_address;

        // Return jump target address (if have it)
        if ((opcode.Flags & OpCodeFlags.Jumps) != 0 && have_jump_address)
        {
          dissassembled_instruction.NextAddress2 = jump_address;
        }
      }
      else
      {
        dissassembled_instruction.NextAddress1 = in_address;
      }

      if (dissassembled_instruction.TStates == dissassembled_instruction.TStates2)
        dissassembled_instruction.TStates2 = 0;

      dissassembled_instruction.Length = (ushort)(in_address - start_address);

      return dissassembled_instruction;

    }

    public OpCode GetCurrentOpcode(ushort in_address)
    {
      OpCode opcode = null;

      byte opc = DisassemblerReadByte(in_address++, null);

      switch (opc)
      {
        case 0xDD:
        case 0xFD:
          byte next = DisassemblerReadByte(in_address++, null);
          if ((next | 0x20) == 0xFD || next == 0xED)
          {
            opcode = null;
          }
          else if (next == 0xCB)
          {
            DisassemblerReadByte(in_address++, null);
            next = DisassemblerReadByte(in_address++, null);

            opcode = (opc == 0xDD) ? dasm_ddcb[next] : dasm_fdcb[next];
          }
          else
          {
            opcode = (opc == 0xDD) ? dasm_dd[next] : dasm_fd[next];
            if (opcode.Mnemonic == null) //mirrored instructions
            {
              opcode = dasm_base[next];
            }
          }
          break;

        case 0xED:
          next = DisassemblerReadByte(in_address++, null);
          opcode = dasm_ed[next];
          if (opcode.Mnemonic == null)
          {
            opcode = null;
          }
          break;

        case 0xCB:
          next = DisassemblerReadByte(in_address++, null);
          opcode = dasm_cb[next];
          break;

        default:
          opcode = dasm_base[opc];
          break;
      }

      return opcode;
    }

    /// <summary>
    /// Gets current instruction length in bytes
    /// </summary>
    /// <param name="in_address">Address where the current instruction starts</param>
    /// <returns>Length in bytes</returns>
    public uint GetCurrentInstructionLength(ushort in_address)
    {
      uint length = 0;

      byte opc = DisassemblerReadByte(in_address++);
      switch (opc)
      {
        case 0xDD:
        case 0xFD:
          byte next = DisassemblerReadByte(in_address++);
          if ((next | 0x20) == 0xFD || next == 0xED)
          {
            length = 2;
          }
          else if (next == 0xCB)
          {
            DisassemblerReadByte(in_address++);
            next = DisassemblerReadByte(in_address++);

            length = ((opc == 0xDD) ? dasm_ddcb[next] : dasm_fdcb[next]).Length;
          }
          else
          {
            length = ((opc == 0xDD) ? dasm_dd[next] : dasm_fd[next]).Length;
          }
          break;

        case 0xED:
          next = DisassemblerReadByte(in_address++);
          length = dasm_ed[next].Length;
          break;

        case 0xCB:
          next = DisassemblerReadByte(in_address++);
          length = dasm_cb[next].Length;
          break;

        default:
          length = dasm_base[opc].Length;
          break;
      }

      return length;
    }

    /// <summary>
    /// Converts word instruction argument to a displayable string
    /// </summary>
    /// <param name="in_word">Value to convert</param>
    /// <returns>Displayable string</returns>
    public string FormatWord(ushort in_word)
    {
      switch (HexConstDisplayMode)
      {
        case HexConstDisplay.Postfix:
          return string.Format("{0:X04}h", in_word);

        case HexConstDisplay.Prefix:
          return string.Format("${0:X04}", in_word);

        default:
          return string.Format("{0:X04}", in_word);
      }
    }

    /// <summary>
    /// Converts byte instruction argument to a displayable string
    /// </summary>
    /// <param name="in_byte">Value to convert</param>
    /// <returns>Displayable string</returns>
    public string FormatByte(byte in_byte)
    {
      switch (HexConstDisplayMode)
      {
        case HexConstDisplay.Postfix:
          return string.Format("{0:X02}h", in_byte);

        case HexConstDisplay.Prefix:
          return string.Format("${0:X02}", in_byte);

        default:
          return string.Format("{0:X02}", in_byte);
      }
    }

    /// <summary>
    /// Converts signed byte instruction argument to a displayable string
    /// </summary>
    /// <param name="in_sbyte">Value to convert</param>
    /// <returns>Displayable string</returns>
    public string FormatSignedByte(sbyte in_sbyte)
    {
      if (in_sbyte < 0)
        return "-" + FormatByte((byte)-in_sbyte);
      else
        return FormatByte((byte)in_sbyte);
    }

    /// <summary>
    /// Converts 16-bit address to a displayable string
    /// </summary>
    /// <param name="in_address">Value to convert</param>
    /// <returns>Displayable string</returns>
    public string FormatAddr(ushort in_address)
    {
      return FormatWord(in_address);
    }

    private byte DisassemblerReadByte(ushort in_address, Z80DisassemblerInstruction inout_instruction = null)
    {
      byte data = ReadByte((ushort)(in_address - AddressOffset));

      if (inout_instruction != null)
      {
        if (!string.IsNullOrEmpty(inout_instruction.Bytes))
          inout_instruction.Bytes += " " + string.Format("{0:X02}", data);
        else
          inout_instruction.Bytes = string.Format("{0:X02}", data);
      }

      return data;
    }
  }
}
