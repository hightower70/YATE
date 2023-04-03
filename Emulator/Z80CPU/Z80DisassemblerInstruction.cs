namespace YATE.Emulator.Z80CPU
{
  public class Z80DisassemblerInstruction
  {
    private string m_asm;

    public string Asm { get { return m_asm; } set { m_asm = value; AsmUpdated(); } }
    public string Comment { get; set; }
    public string Bytes { get; set; }
    public uint TStates;
    public uint TStates2;
    public ushort Length;
    public int Address { get; set; } = -1;

    public ushort? NextAddress1;
    public ushort? NextAddress2;
    public ushort? WordVal;
    public byte? ByteVal;
    public string Label { set; get; } = string.Empty;

    public byte NumericOperand { get; set; }
    public Z80DisassemblerTable.OpCode OpCode;

    /// <summary>
    /// Gets mnemonic string e.g. 'LD' from 'LD A,1' instruction
    /// </summary>
    /// <returns></returns>
    public string Mnemonic { get; set; } = string.Empty;

    /// <summary>
    /// Gets operand1 in string e.g. 'A' from 'LD A,1' instruction
    /// </summary>
    /// <returns></returns>
    public string Operand1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets operand2 string e.g. '1' from 'LD A,1' instruction
    /// </summary>
    /// <returns></returns>
    public string Operand2 { get; set; } = string.Empty;

    public byte JumpOperand
    {
      get
      {
        if (OpCode == null)
          return 0;

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
        if (TStates == 0)
          return string.Empty;

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

    public void AsmUpdated()
    {
      int space_pos;
      string operand;

      // set mnemonic
      space_pos = m_asm.IndexOf(' ');

      if (space_pos > 0)
        Mnemonic = m_asm.Substring(0, space_pos);
      else
        Mnemonic = m_asm;

      // set operand 1
      if (space_pos > 0)
      {
        operand = m_asm.Substring(space_pos + 1);

        int comma_pos = operand.IndexOf(',');
        if (comma_pos >= 0)
        {
          Operand1 = operand.Substring(0, comma_pos);
          Operand2 = operand.Substring(comma_pos + 1).Trim();
        }
        else
        {
          Operand1 = operand;
        }
      }
    }

    public void AppendToOperand(int in_operand_index, string in_string_to_append)
    {
      if(in_operand_index == 1)
      {
        if (string.IsNullOrEmpty(Operand1))
          Operand1 = in_string_to_append;
        else
          Operand1 += in_string_to_append;
      }
      else
      {
        if (string.IsNullOrEmpty(Operand2))
          Operand2 = in_string_to_append;
        else
          Operand2 += in_string_to_append;
      }
    }
  }
}

