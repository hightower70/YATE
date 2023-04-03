using System.Collections.Generic;
using System.IO;

namespace YATE.Disassembler
{
  public class AssemblerListReader
  {
    private List<AssemblerListParserBase> m_parsers;
    private AssemblerListParserBase m_current_parser;
    private List<DisassemblyLine> m_disassembly_lines;
    private int m_line_count;

    public AssemblerListReader()
    {
      m_parsers = new List<AssemblerListParserBase>();
      m_disassembly_lines = new List<DisassemblyLine>();
    }

    public int LineCount { get { return m_line_count; } }

    public List<DisassemblyLine> Disassembly { get { return m_disassembly_lines; } }

    public void RegisterParser(AssemblerListParserBase in_parser)
    {
      m_parsers.Add(in_parser);
    }

    public void ReadAssemblyList(string in_file_name)
    {
      int i;
      string current_line;
      m_current_parser = null;
      List<DisassemblyLine> result = new List<DisassemblyLine>();

      // first pass -> determine file type
      using (StreamReader list_file = new StreamReader(in_file_name))
      {
        // open detection
        for (i = 0; i < m_parsers.Count; i++)
          m_parsers[i].OpenListFileTypeDetection();

        // read file
        current_line = list_file.ReadLine();

        // Continue to read until we reach end of file
        while (current_line != null && m_current_parser == null)
        {
          for (i = 0; i < m_parsers.Count; i++)
          {
            if (m_parsers[i].DetectListFileType(current_line))
            {
              m_current_parser = m_parsers[i];
              break;
            }
          }

          //Read the next line
          current_line = list_file.ReadLine();
        }

        // close  detection
        for (i = 0; i < m_parsers.Count; i++)
          m_parsers[i].CloseListFileTypeDetection();

        // close file
        list_file.Close();
      }

      // check file type
      if (m_current_parser == null)
        return;

      // open parser
      m_current_parser.OpenParser(m_disassembly_lines);

      // reopen file
      m_line_count = 1;
      using (StreamReader list_file = new StreamReader(in_file_name))
      {
        // Continue to read until we reach end of file
        do
        {
          //Read the next line
          current_line = list_file.ReadLine();

          if (current_line != null)
          {
            m_current_parser.ParseLine(current_line);
            m_line_count++;
          }
        } while (current_line != null);

        // close parser
        m_current_parser.CloseParser();

        // close file
        list_file.Close();
      }
    }
  }
}

