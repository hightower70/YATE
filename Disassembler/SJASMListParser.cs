using System.Collections.Generic;
using System.Globalization;
using YATE.Emulator.Z80CPU;

namespace YATE.Disassembler
{
  class SJASMListParser : AssemblerListParserBase
  {
    private const int BytesFieldWidth = 16;
    private const int MnemonicWidth = 4;

    private enum ParserMode
    {
      Asm,
      Label
    };

    private ParserMode m_parser_mode;
    private Z80Disassembler m_disassembler;

    public SJASMListParser() : base()
    {
      m_parser_mode = ParserMode.Asm;

      AssemblerDirectives = new HashSet<string>
      {
        "ABYTE", "ABYTEC", "ABYTEZ", "ALIGN", "ASSERT",
        "BINARY", "BLOCK", "BPLIST", "BYTE",
        "CSPECTMAP" ,
        "D24" , "DB", "DC", "DD", "DEFARRAY", "DEFARRAY+", "DEFB", "DEFD", "DEFDEVICE", "DEFG", "DEFH", "DEFINE+", "DEFINE", "DEFL", "DEFM", "DEFS", "DEFW", "DEPHASE", "DEVICE", "DG", "DH", "DISP", "DISPLAY" , "DM", "DS", "DUP", "DW", "DWORD", "DZ",
        "EMPTYTAP", "EMPTYTRD", "ELSE", "ELSEIF", "ENDIF", "ENCODING", "END", "ENDLUA", "ENDMOD", "ENDMODULE", "ENDT", "ENT", "EQU", "EXPORT",
        "FPOS",
        "HEX",
        "INCBIN", "IF", "IFN", "IFDEF", "IFNDEF", "IFUSED", "IFNUSED", "INCHOB", "INCLUDE", "INCLUDELUA", "INCTRD", "INSERT",
        "LABELSLIST", "LUA",
        "MACRO", "MEMORYMAP", "MMU", "MODULE", "OPT", "ORG", "OUTEND", "OUTPUT",
        "PAGE", "PHASE",
        "RELOCATE_END", "RELOCATE_START", "RELOCATE_TABLE", "REPT",
        "SAVEBIN", "SAVECPCSNA", "SAVEDEV" , "SAVEHOB", "SAVENEX", "SAVESNA", "SAVETAP", "SAVETRD", "SETBP", "SETBREAKPOINT", "SHELLEXEC" , "SIZE", "SLDOPT", "SLOT",
        "TAPEND", "TAPOUT" , "TEXTAREA",
        "UNDEFINE", "UNPHASE",
        "WHILE", "WORD"
      };

      m_disassembler = new Z80Disassembler();
      m_disassembler.ReadByte = ReadByte;
    }

    private byte ReadByte(ushort in_address)
    {
      return MemoryBuffer[in_address];
    }

    public override void OpenParser(List<DisassemblyLine> in_disassembly_line)
    {
      base.OpenParser(in_disassembly_line);

      m_parser_mode = ParserMode.Asm;
    }

    public override bool DetectListFileType(string in_line)
    {
      // determine type 
      if (in_line.StartsWith("# file opened:"))
        return true;

      return false;
    }

    public override void ParseLine(string in_line)
    {
      // skip '# file' lines
      if (in_line.StartsWith("# file"))
        return;

      // store current line
      m_current_line = in_line;

      // determine line type
      if (in_line.StartsWith("Value    Label"))
      {
        m_parser_mode = ParserMode.Label;
        return;
      }

      switch (m_parser_mode)
      {
        case ParserMode.Asm:
          ParseAsmLine();
          break;

        case ParserMode.Label:
          ParseLabelLine();
          break;
      }
    }

    private void ParseAsmLine()
    {
      DisassemblyLine disassembly_line = null;

      m_source_start_column = 24;
      m_token_pos = 0;

      int pos;

      ConvertTabToSpaces();

      // try to get line number
      SkipSpaces();
      pos = m_token_pos;
      FindWhitespace();

      string line_number;

      // skip empty lines
      if (m_token_pos == 0)
        return;

      if (char.IsNumber(m_current_line[m_token_pos - 1]))
      {
        line_number = m_current_line.Substring(pos, m_token_pos - pos);
      }
      else
      {
        line_number = m_current_line.Substring(pos, m_token_pos - 1 - pos);
      }

      if (string.IsNullOrEmpty(line_number) || !char.IsDigit(line_number[0]))
        return;

      //out_line.LineNumber = ushort.Parse(line_number);

      // try to get address
      SkipSpaces();
      pos = m_token_pos;
      FindWhitespace();

      int address = 0;
      string address_string = m_current_line.Substring(pos, m_token_pos - pos);
      if (string.IsNullOrEmpty(address_string) || !int.TryParse(address_string, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address))
        return;

      // load hex bytes (if existing)
      pos = m_token_pos;
      if (m_token_pos >= m_current_line.Length || m_current_line[m_token_pos] != ' ')
        return;

      m_token_pos++;
      if (m_token_pos >= m_current_line.Length)
        return;

      int data;
      int data_count = 0;
      string bytes = "";

      while (data_count < 4 && m_current_line.Length >= (m_token_pos + 2) && int.TryParse(m_current_line.Substring(m_token_pos, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out data))
      {
        if (address + data_count < MemoryBuffer.Length)
          MemoryBuffer[address + data_count] = (byte)data;

        if (bytes.Length > 0)
          bytes += " ";

        bytes += data.ToString("X2");

        if (m_current_line.Length > m_token_pos + 2 && m_current_line[m_token_pos + 2] != ' ')
          return;

        m_token_pos += 3;

        data_count++;
      }

      SkipSpaces();

      disassembly_line = new DisassemblyLine();
      int operand_index = 1;
      bool directive = false;

      // parse line
      while (m_token_pos < m_current_line.Length)
      {
        GetNextToken();

        switch (m_token_type)
        {
          case TokenType.Mnemonic:
            if (data_count > 0)
            {
              disassembly_line.DisassemblyInstruction = m_disassembler.Disassemble((ushort)address);
            }
            disassembly_line.DisassemblyInstruction.Mnemonic = m_current_token.PadRight(4);
            disassembly_line.DisassemblyInstruction.Operand1 = string.Empty;
            disassembly_line.DisassemblyInstruction.Operand2 = string.Empty;
            break;

          case TokenType.Label:
            disassembly_line.DisassemblyInstruction.Label = m_current_token.PadRight(BytesFieldWidth);
            break;

          case TokenType.Directive:
            disassembly_line.DisassemblyInstruction.Mnemonic = m_current_token.PadRight(MnemonicWidth);
            if (string.IsNullOrEmpty(disassembly_line.DisassemblyInstruction.Label) && string.IsNullOrEmpty(disassembly_line.DisassemblyInstruction.Operand1))
              disassembly_line.DisassemblyInstruction.Label = disassembly_line.DisassemblyInstruction.Operand1;
            directive = true;
            break;

          case TokenType.Operator:
            if (directive)
            {
              disassembly_line.DisassemblyInstruction.AppendToOperand(1, m_current_token);
              if (m_current_token.Length == 1 && m_current_token[0] == ',')
                disassembly_line.DisassemblyInstruction.AppendToOperand(1, " ");

            }
            else
            {
              if (m_current_token.Length == 1)
              {
                switch (m_current_token[0])
                {
                  case ',':
                    if (operand_index == 1)
                      operand_index = 2;
                    else
                      disassembly_line.DisassemblyInstruction.AppendToOperand(operand_index, m_current_token);
                    break;

                  case ')':
                  case '(':
                    disassembly_line.DisassemblyInstruction.AppendToOperand(operand_index, m_current_token);
                    break;
                }
              }
            }
            break;

          case TokenType.Comment:
            disassembly_line.DisassemblyInstruction.Comment = m_current_token;
            break;

          default:
            disassembly_line.DisassemblyInstruction.AppendToOperand(operand_index, m_current_token);
            break;
        }
      }

      if (disassembly_line != null)
      {
        if (string.IsNullOrEmpty(disassembly_line.DisassemblyInstruction.Bytes))
        {
          disassembly_line.DisassemblyInstruction.Bytes = new string(' ', BytesFieldWidth);
        }
        else
        {
          disassembly_line.DisassemblyInstruction.Bytes = disassembly_line.DisassemblyInstruction.Bytes.PadRight(BytesFieldWidth);
        }

        m_disassembly_lines.Add(disassembly_line);
      }
    }

    private void ParseLabelLine()
    {

    }
  }
}
