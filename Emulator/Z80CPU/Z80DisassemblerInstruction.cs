using System;

namespace YATE.Emulator.Z80CPU
{
  public class Z80DisassemblerInstruction
  {
    public string Asm;
    public string Comment;
    public string Bytes { get; set; }
    public uint TStates;
    public uint TStates2;
    public ushort Length;
    public ushort Address { get; set; }
    public ushort? NextAddress1;
    public ushort? NextAddress2;
    public ushort? WordVal;
    public byte? ByteVal;
    public byte NumericOperand { get; set; }
    public Z80DisassemblerTable.OpCode OpCode;

    /// <summary>
    /// Gets mnemonic string e.g. 'LD' from 'LD A,1' instruction
    /// </summary>
    /// <returns></returns>
    public string Mnemonic
    {
      get
      {
        string mnemonic = string.Empty;

        int space_pos = Asm.IndexOf(' ');

        if (space_pos > 0)
          mnemonic = Asm.Substring(0, space_pos);
        else
          mnemonic = Asm;

        return mnemonic;
      }
    }

    /// <summary>
    /// Gets operand1 in string e.g. 'A' from 'LD A,1' instruction
    /// </summary>
    /// <returns></returns>
    public string Operand1
    {
      get
      {
        string operand = string.Empty;

        int space_pos = Asm.IndexOf(' ');

        if (space_pos >= 0)
        {
          operand = Asm.Substring(space_pos + 1);

          int comma_pos = operand.IndexOf(',');
          if (comma_pos >= 0)
            operand = operand.Substring(0, comma_pos);
        }

        return operand;
      }
    }

    /// <summary>
    /// Gets operand2 string e.g. '1' from 'LD A,1' instruction
    /// </summary>
    /// <returns></returns>
    public string Operand2
    {
      get
      {
        string operand = string.Empty;

        int comma_pos = Asm.IndexOf(',');
        if (comma_pos >= 0)
        {
          operand = Asm.Substring(comma_pos + 1).Trim();
        }

        return operand;
      }
    }

    public byte JumpOperand
    {
      get
      {
        if (OpCode.Flags.HasFlag(Z80DisassemblerTable.OpCodeFlags.Jumps))
        {
          return NumericOperand;
        }
        else
        {
          return 0;
        }
      }
    }


    public string TstatesString
    {
      get
      {
        if (TStates2 == 0)
        {
          return TStates.ToString();
        }
        else
        {
          return TStates.ToString() + '/' + TStates2.ToString();
        }
      }
    }

    public string CommentString
    {
      get
      {
        if (ByteVal != null && (byte)ByteVal >= 32 && (byte)ByteVal < 128)
        {
          return "; '" + (char)ByteVal + "'";
        }

        return string.Empty;
      }
    }
  }
}
