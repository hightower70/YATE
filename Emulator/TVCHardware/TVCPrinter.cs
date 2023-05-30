///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019 Laszlo Arvai. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
// MA 02110-1301  USA
///////////////////////////////////////////////////////////////////////////////
// File description
// ----------------
// Videoton TV Computer Printer Emulation
///////////////////////////////////////////////////////////////////////////////
using System.IO;
using System.Text;
using System.Windows.Shapes;
using YATECommon;

namespace YATE.Emulator.TVCHardware
{
  /// <summary>
  /// Tape hardware emulation
  /// </summary>
  public class TVCPrinter
  {
    private TVComputer m_tvc;
    private byte m_data_register;
    private string m_printer_file_name;
    private StringBuilder m_line_buffer;
    private int m_dstrb;
    private int m_nack;
    private ulong m_timestamp;
    private ulong m_ack_delay;

    public string PrinterFileName
    {
      get { return m_printer_file_name; }
      set { m_printer_file_name = value; }
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="in_tvc"></param>
    public TVCPrinter(TVComputer in_tvc)
    {
      m_tvc = in_tvc;
      m_printer_file_name = null;
      m_data_register = 0;
      m_line_buffer = new StringBuilder();
      m_dstrb = 1;
      m_nack = 1; 
      m_timestamp = 0;
      m_ack_delay = m_tvc.MicrosecToCPUTicks(10);

      m_tvc.Ports.AddPortWriter(0x01, PortWrite01H);
      m_tvc.Ports.AddPortReset(0x01, PortReset01H);

      m_tvc.Ports.AddPortWriter(0x06, PortWrite06H);
      m_tvc.Ports.AddPortReset(0x06, PortReset06H);

      m_tvc.Ports.AddPortReader(0x5B, PortRead59H);
    }



    // PORT 01H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |                   P R I N T E R  D A T A                      |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |   D7  |   D6  |   D5  |   D4  |   D3  |   D2  |   D1  |   D0  |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    private void PortWrite01H(ushort in_address, byte in_data)
    {
      m_data_register = in_data;
    }

    private void PortReset01H()
    {
      m_data_register = 0;
    }

    // PORT 06H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |                   P R I N T E R  S T R O B E                  |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |NDSTRB |   -   |   +   |   +   |   +   |   +   |   +   |   +   |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    private void PortWrite06H(ushort in_address, byte in_data)
    {
      int dstrb = (in_data >> 7) & 0x01;

      // DSTRB falling edge
      if(m_dstrb != 0 && dstrb == 0)
      {
        WriteCharacter(m_data_register);

        m_timestamp = m_tvc.GetCPUTicks();
        m_nack = 1;
      }

      m_dstrb = dstrb;
    }

    private void PortReset06H()
    {
      m_dstrb = 1;
    }


    // PORT 59H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |         P R I N T E R  A N D  T A P E  I N                    |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |  NACK |   +   |   +   |   +   |   +   |   +   |   +   |   +   |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    private void PortRead59H(ushort in_address, ref byte inout_data)
    {
      if (m_tvc.GetTicksSince(m_timestamp) > m_ack_delay)
      {
        m_nack = 0;
      }

      inout_data = (byte)((inout_data & 0x7f) | ((m_nack != 0) ? 0x80: 0));
    }


    /// <summary>
    /// Stores printed character into the capture file
    /// </summary>
    /// <param name="in_character"></param>
    private void WriteCharacter(byte in_character)
    {
      // convert to UTF-8
      char tvc_char = TVCCharacterCodePage.TVCCharToUNICODEChar((char)in_character);

      // add character to line buffer (except line end characters)
      if (tvc_char != '\r' && tvc_char != '\r')
        m_line_buffer.Append(tvc_char);

      // write line at the end of the line
      if (tvc_char == '\r')
      {
        // store line
        try
        {
          using (StreamWriter writer = File.AppendText(m_printer_file_name))
          {
            writer.Write(m_line_buffer.ToString());
          }
        }
        catch
        {
        }

        // clear line buffer
        m_line_buffer.Clear();
      }
    }
  }
}
