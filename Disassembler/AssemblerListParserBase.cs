using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YATE.Disassembler
{
  public abstract class AssemblerListParserBase
  {
    protected const int MemoryBufferLength = 65536;

    public byte[] MemoryBuffer { get; private set; }

    protected enum TokenType
    {
      Unknown,

      Identifier,
      Mnemonic,
      Register,
      Directive,
      Label,
      String,
      Numeric,
      Operator,
      Comment
    }



    protected int m_tabstops = 8;

    protected string m_current_line;

    protected int m_source_start_column;
    protected string m_current_token;
    protected string m_current_token_with_whitespaces;
    protected int m_token_start_pos;
    protected int m_token_end_pos;
    protected int m_token_pos;
    protected TokenType m_token_type;
    protected char m_token_string_terminator;
    protected List<DisassemblyLine> m_disassembly_lines;


    protected readonly HashSet<string> Z80Mnemonics = new HashSet<string>
    {
        "ADC",  "ADD",  "AND",  "BIT",  "CALL", "CCF",  "CP",   "CPD",
        "CPDR", "CPI",  "CPIR", "CPL",  "DAA",  "DEC",  "DI",   "DJNZ",
        "EI",   "EX",   "EXX",  "HALT", "IM",   "IN",   "INC",  "IND",
        "INDR", "INI",  "INIR", "JP",   "JR",   "LD",   "LDD",  "LDDR",
        "LDI",  "LDIR", "NEG",  "NOP",  "OR",   "OTDR", "OTIR", "OUT",
        "OUTD", "OUTI", "POP",  "PUSH", "RES",  "RET",  "RETI", "RETN",
        "RL",   "RLA",  "RLC",  "RLCA", "RLD",  "RR",   "RRA",  "RRCA",
        "RRD",  "RST",  "SBC",  "SCF",  "SET",  "SLA",  "SRA",  "SRL",
        "SUB",  "XOR"
    };

    protected readonly HashSet<string> Z80Registers = new HashSet<string>
    {
        "A",  "B",  "C",  "D",  "E", "H",  "L",
        "AF", "BC",  "DE", "HL",  "IX",  "IY",
        "AF'", "SP"
    };

    protected HashSet<string> AssemblerDirectives;

    public AssemblerListParserBase()
    {
      MemoryBuffer = new byte[MemoryBufferLength];

      for (int i = 0; i < MemoryBuffer.Length; i++)
      {
        MemoryBuffer[i] = 0xff;

      }
    }

    public virtual void OpenListFileTypeDetection()
    {

    }

    public abstract bool DetectListFileType(string in_line);

    public void CloseListFileTypeDetection()
    {

    }


    public virtual void OpenParser(List<DisassemblyLine> in_disassembly_line)
    {
      m_disassembly_lines = in_disassembly_line;
    }

    public abstract void ParseLine(string in_line);

    public virtual void CloseParser()
    {

    }

    protected void ConvertTabToSpaces()
    {
      StringBuilder new_line=new StringBuilder(m_current_line.Length);

      for (int pos = 0; pos < m_current_line.Length; pos++)
      {
        if (m_current_line[pos] == '\t')
        {
          new_line.Append(new string(' ', m_tabstops - pos % m_tabstops));
        }
        else
          new_line.Append(m_current_line[pos]);
      }

      m_current_line = new_line.ToString();
    }



    protected void SkipSpaces()
    {
      while (m_token_pos < m_current_line.Length && m_current_line[m_token_pos] == ' ')
        m_token_pos++;
    }

    protected void FindWhitespace()
    {
      while (m_token_pos < m_current_line.Length && m_current_line[m_token_pos] != ' ')
        m_token_pos++;
    }

    protected bool IsHexDigit(char in_char)
    {
      return (in_char >= '0' && in_char <= '9') || (in_char >= 'a' && in_char <= 'f') || (in_char >= 'A' && in_char <= 'F');
    }

    protected void GetNextToken()
    {
      bool token_end = false;

      m_token_type = TokenType.Unknown;
      m_token_start_pos = m_token_pos;

      // skip leading whitespaces
      while (m_token_pos < m_current_line.Length && char.IsWhiteSpace(m_current_line[m_token_pos]))
        m_token_pos++;

      // determine token type
      switch (m_current_line[m_token_pos])
      {
        case '\'':
          m_token_type = TokenType.String;
          m_token_string_terminator = '\'';
          m_token_pos++;
          break;

        case '\"':
          m_token_type = TokenType.String;
          m_token_string_terminator = '\"';
          m_token_pos++;
          break;

        case ';':
          m_token_type = TokenType.Comment;
          break;

        case '/':
          if (m_token_pos + 1 < m_current_line.Length && m_current_line[m_token_pos + 1] == '/')
            m_token_type = TokenType.Comment;
          else
          {
            m_token_type = TokenType.Operator;
            token_end = true;
          }
          break;

        case '$':
          if (m_token_pos + 1 < m_current_line.Length && IsHexDigit(m_current_line[m_token_pos + 1]))
          {
            m_token_type = TokenType.Numeric;
            m_token_pos++;
          }
          else
          {
            m_token_type = TokenType.Operator;
            token_end = true;
            m_token_pos++;
          }
          break;

        case '.':
          if (m_token_pos + 1 < m_current_line.Length && char.IsLetter(m_current_line[m_token_pos + 1]))
          {
            m_token_type = TokenType.Identifier;
            m_token_pos++;
          }
          break;

        case '(':
        case ')':
        case ',':
        case '!':
        case '~':
        case '+':
        case '-':
        case '*': 
        case '%':
        case '<':
        case '>':
        case '?':
        case '=':
        case '&':
        case '|':
          m_token_type = TokenType.Operator;
          token_end = true;
          m_token_pos++;
          break;

        case '0':
          if (m_token_pos + 1 < m_current_line.Length && (m_current_line[m_token_pos + 1] == 'x' || m_current_line[m_token_pos + 1] == 'X'))
          {
            m_token_pos += 2;
          }
          m_token_type = TokenType.Numeric;
          break;

        default:
          if (char.IsLetter(m_current_line[m_token_pos]))
          {
            m_token_type = TokenType.Identifier;
          }
          else
          {
            if (char.IsNumber(m_current_line[m_token_pos]))
              m_token_type = TokenType.Numeric;
            else
            {

            }
          }
          break;
       }


      // process token
      char ch;
      while (m_token_pos < m_current_line.Length && !token_end)
      {
        ch = m_current_line[m_token_pos];
        switch (m_token_type)
        {
          case TokenType.Identifier:
            if (ch == '\'')
            {
              // Accept af'
              if(!(m_token_pos>1 && m_current_line.Substring(m_token_pos-2, 2).ToUpper()=="AF"))
              {
                token_end = true;
              }
            }
            else
            {
              if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                token_end = true;
            }
            break;

          case TokenType.Numeric:
            if (!IsHexDigit(ch))
            {
              token_end = true;
            }
            break;

          case TokenType.String: // copy all characters to the next " character
            if (ch == '\"')
            {
              m_token_pos++;
              token_end = true;
            }
            break;

          case TokenType.Comment: // copy all characters to the end of line
            break;
        }

        if (!token_end)
          m_token_pos++;
      }

      // change token type to label if it contains 
      if (m_token_type == TokenType.Identifier)
      {
        if (m_token_start_pos == m_source_start_column)
        {
          m_token_type = TokenType.Label;
        }

        if (m_token_pos < m_current_line.Length && m_current_line[m_token_pos] == ':')
        {
          m_token_type = TokenType.Label;
          m_token_pos++;
        }
      }

      // create token strings
      m_current_token_with_whitespaces = m_current_line.Substring(m_token_start_pos, m_token_pos - m_token_start_pos);
      m_current_token = m_current_token_with_whitespaces.Trim();

      // check reserved words
      if (m_token_type == TokenType.Identifier)
      {
        string current_token_upper = m_current_token.ToUpper();

        // search for mnemonics
        if (Z80Mnemonics.Contains(current_token_upper))
        {
          m_token_type = TokenType.Mnemonic;
        }
        else
        {
          // search for registers
          if (Z80Registers.Contains(current_token_upper))
          {
            m_token_type = TokenType.Register;
          }
          else
          {
            // search for directives
            if (AssemblerDirectives.Contains(current_token_upper))
            {
              m_token_type = TokenType.Directive;
            }
            else
            {
              // accept directives with dot
              if (m_current_token.Length > 0 && m_current_token[0] == '.')
              {
                if (AssemblerDirectives.Contains(current_token_upper.Substring(1)))
                {
                  m_token_type = TokenType.Directive;
                }
              }
            }
          }
        }
      }
    }
  }
}
