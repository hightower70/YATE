﻿///////////////////////////////////////////////////////////////////////////////
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
// Videoton TV Computer Video Hardware Emulation
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;

namespace YATE.Emulator.TVCHardware
{
  public class TVCVideo
  {
    #region · Constants ·

    /// <summary>
    /// 6845 CRT controller registers
    /// </summary>
    public const int MC6845_HTOTAL = 0;
    public const int MC6845_HDISPLAYED = 1;
    public const int MC6845_HSYNCPOS = 2;
    public const int MC6845_SYNCWIDTHS = 3;
    public const int MC6845_VTOTAL = 4;
    public const int MC6845_VTOTALADJ = 5;
    public const int MC6845_VDISPLAYED = 6;
    public const int MC6845_VSYNCPOS = 7;
    public const int MC6845_INTERLACEMODE = 8;
    public const int MC6845_MAXSCANLINEADDR = 9;
    public const int MC6845_CURSORSTART = 10;
    public const int MC6845_CURSOREND = 11;
    public const int MC6845_STARTADDRHI = 12;
    public const int MC6845_STARTADDRLO = 13;
    public const int MC6845_CURSORHI = 14;
    public const int MC6845_CURSORLO = 15;
    public const int MC6845_LIGHTPENHI = 16;
    public const int MC6845_LIGHTPENLO = 17;

    public const int MC6845RegisterCount = 18;

    public const int PaletterColorCount = 4;

    public const byte DimScanlineColorMultiplier = 192;

    public const byte HalfIntensity = 128;

    public const int HorizontalTiming = 64000; // horizontal timing in ns
    public const int ScanlineVSyncPackPorch = 24; // first visible scanline after vertical sync and back porch
    public const int PixelHSyncPackPorch = 130; // first visible column after horizonjtal sync and back porch

    public readonly Color[] TVCColors =
    {
      Color.FromArgb(255, 0, 0, 0),				// Black
			Color.FromArgb(255, 0, 0, HalfIntensity),			// DarkBlue
			Color.FromArgb(255, HalfIntensity, 0, 0),			// DarkRed
			Color.FromArgb(255, HalfIntensity, 0, HalfIntensity),		// DarkPurple
			Color.FromArgb(255, 0, HalfIntensity, 0),			// DarkGreen
			Color.FromArgb(255, 0, HalfIntensity, HalfIntensity),		// DarkCyan
			Color.FromArgb(255, HalfIntensity, HalfIntensity, 0),		// DarkYellow
			Color.FromArgb(255, HalfIntensity, HalfIntensity, HalfIntensity), // DarkGray

			Color.FromArgb(255, 0, 0, 0),				// Black
			Color.FromArgb(255, 0, 0, 255),			// LightBlue
			Color.FromArgb(255, 255, 0, 0),			// LightRed
			Color.FromArgb(255, 255, 0, 255),		// LightPurple
			Color.FromArgb(255, 0, 255, 0),			// LightGreen
			Color.FromArgb(255, 0, 255, 255),		// LightCyan
			Color.FromArgb(255, 255, 255, 0),		// LightYellow
			Color.FromArgb(255, 255, 255, 255)	// White
		};

    #endregion

    #region · Types ·


    [StructLayout(LayoutKind.Explicit)]
    public class FrameBuffer
    {
      [FieldOffset(0)]
      public int NumberOfBytes;

      [FieldOffset(8)]
      public byte[] ByteBuffer;

      [FieldOffset(8)]
      public uint[] UIntBuffer;
    }

    /// <summary>
    /// Event parameter for frame ready event
    /// </summary>
    public class FrameReadyEventparam
    {
      public int Width { set; get; } // Width in pixels of the decoded image frame
      public int Height { set; get; } // Width in pixels of the decoded image frame
      public byte[] FrameData; // image data in RGB24 format withour any padding
    };

    #endregion

    #region · Data Members ·

    // TVC hardware
    private TVComputer m_tvc;

    // image properties
    private int m_image_width;
    private int m_image_height;

    // frame buffer
    private uint m_frame_counter;
    private FrameBuffer m_frame_buffer;

    private FrameReadyEventparam m_frame_ready_event_param;
    private SynchronizationContext m_context;

    // 6845 CRT registers
    private readonly byte[] m_6845_registers;
    private byte m_6845_address;

    // ports
    private byte m_port_00h;
    private byte m_port_06h;
    private byte[] m_port_palette;

    // color related variables
    private uint[] m_current_colors;
    private uint[] m_current_graphics16_colors;
    private uint[] m_current_graphics16_dim_colors;

    // video refresh variables
    private int m_scanline_index = 0;
    private int m_RA = 0;
    private int m_MA = 0;



    private Random m_rand = new Random();

    #endregion

    #region · Properties ·

    public uint FrameCounter
    {
      get { return m_frame_counter; }
    }

    public byte[] M6845Registers
    {
      get { return m_6845_registers; }
    }

    public byte M6845RegisterAddress
    {
      get { return m_6845_address; }
      set { m_6845_address = value; }
    }

    public bool BlackAndWhite
    {
      get; set;
    }

    #endregion

    #region · Event handler ·

    /// <summary>
    /// Event handler called at every refreshed frame
    /// </summary>
    public event EventHandler<FrameReadyEventparam> FrameReady;

    #endregion

    public TVCVideo(TVComputer in_tvc)
    {
      m_tvc = in_tvc;

      BlackAndWhite = false;

      m_context = SynchronizationContext.Current;

      // color tables init
      m_current_graphics16_colors = new uint[128];
      m_current_graphics16_dim_colors = new uint[128];
      m_current_colors = new uint[TVCColors.Length];
      FillColorCache();


      m_6845_registers = new byte[MC6845RegisterCount];
      m_port_palette = new byte[PaletterColorCount];

      m_tvc.Ports.AddPortWriter(0x00, PortWrite00H);

      m_tvc.Ports.AddPortWriter(0x06, PortWrite06H);

      m_tvc.Ports.AddPortWriter(0x60, PortWrite60H);
      m_tvc.Ports.AddPortWriter(0x61, PortWrite61H);
      m_tvc.Ports.AddPortWriter(0x62, PortWrite62H);
      m_tvc.Ports.AddPortWriter(0x63, PortWrite63H);

      m_tvc.Ports.AddPortWriter(0x70, PortWrite70H);
      m_tvc.Ports.AddPortWriter(0x71, PortWrite71H);

      m_frame_ready_event_param = new FrameReadyEventparam();

      AllocateFrameBuffer(640, 576);
    }

    public void Reset()
    {
      m_frame_counter = 0;
      m_scanline_index = 0;
      m_RA = 0;
      m_MA = 0;
    }

    #region · Port handlers ·

    // PORT 06H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |                 G R A P H I C S  M O D E                      |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |   +   |   +   |  -    |   -   |   -   |   -   |  00:  2 color |
    // |       |       |       |       |       |       |  01:  4 color |
    // |       |       |       |       |       |       |  1x: 16 color |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    public void PortWrite06H(ushort in_address, byte in_data)
    {
      m_port_06h = (byte)(in_data & 0x03);
    }

    // PORT 00H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |                  B O R D E R  C O L O R                       |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |   I   |   -   |   G   |   -   |   R   |   -   |   B   |   -   |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    public void PortWrite00H(ushort in_address, byte in_data)
    {
      m_port_00h = in_data;
    }

    public void PortWrite60H(ushort in_address, byte in_data)
    {
      m_port_palette[0] = in_data;
    }

    public void PortWrite61H(ushort in_address, byte in_data)
    {
      m_port_palette[1] = in_data;
    }

    public void PortWrite62H(ushort in_address, byte in_data)
    {
      m_port_palette[2] = in_data;
    }

    public void PortWrite63H(ushort in_address, byte in_data)
    {
      m_port_palette[3] = in_data;
    }

    public void PortWrite70H(ushort in_address, byte in_data)
    {
      m_6845_address = (byte)(in_data & 0x1f);
    }

    public void PortWrite71H(ushort in_address, byte in_data)
    {
      m_6845_registers[m_6845_address] = in_data;
    }

    #endregion

    private void AllocateFrameBuffer(int in_width, int in_height)
    {
      m_image_width = in_width;
      m_image_height = in_height;

      m_frame_buffer = new FrameBuffer();
      m_frame_buffer.NumberOfBytes = in_width * in_height * sizeof(uint);
      m_frame_buffer.ByteBuffer = new byte[m_frame_buffer.NumberOfBytes];
    }

    public void UpdateFrame()
    {
      // get it on the UI thread
      m_context.Post(delegate
      {
        // update event data
        m_frame_ready_event_param.Width = m_image_width;
        m_frame_ready_event_param.Height = m_image_height;
        m_frame_ready_event_param.FrameData = m_frame_buffer.ByteBuffer;

        // tell whoever's listening that we have a frame to draw
        FrameReady?.Invoke(this, m_frame_ready_event_param);

      }, null);
    }

    public bool RenderScanline()
    {
      bool render_error = false;

      // determine first visible line
      int first_visible_line = ScanlineVSyncPackPorch;

      // skip non visible scanlines
      if (m_scanline_index < first_visible_line)
      {
        m_scanline_index++;
        return false;
      }

      // Total character count (must be 100 for 15625Hz horizontal sync)
      int total_character_count = m_6845_registers[MC6845_HTOTAL] + 1;
      if (total_character_count != 100)
      {
        render_error = true;
      }

      // Character line count (must be 4 for correct address line generation
      int character_line_count = (m_6845_registers[MC6845_MAXSCANLINEADDR] & 0x1f) + 1;
      if (character_line_count != 4)
      {
        render_error = true;
      }

      // calculate total lines (must be around 312 for 50Hz)
      int total_vertical_lines = ((m_6845_registers[MC6845_VTOTAL] & 0x7f) + 1) * character_line_count + (m_6845_registers[MC6845_VTOTALADJ] & 0x1f);
      if (total_vertical_lines < 310 || total_vertical_lines > 320)
      {
        render_error = true;
        total_vertical_lines = 312;
      }

      int frame_buffer_line_index = m_scanline_index - first_visible_line;
      int frame_buffer_line_length = m_image_width;

      // render scanline
      if (render_error)
      {
        if (frame_buffer_line_index < m_image_height / 2)
        {
          int frame_buffer_pos = frame_buffer_line_index * m_image_width * 2;

          // render nosisy scanline
          uint c;

          // even line
          for (int i = 0; i < m_image_width; i++)
          {
            c = (uint)m_rand.Next(255);

            m_frame_buffer.UIntBuffer[frame_buffer_pos] = 0xff000000u | (c << 16) | (c << 8) | (c);
            m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = 0x80000000u | (c << 16) | (c << 8) | (c);
            frame_buffer_pos++;
          }
        }
      }
      else
      {
        // start rendering
        if (m_scanline_index == first_visible_line)
        {
          m_RA = 0;
          m_MA = ((m_6845_registers[MC6845_STARTADDRHI] & 0x3f) << 8) + m_6845_registers[MC6845_STARTADDRLO];
        }

        // check for display enabled in vertical direction
        int line_start_video_memory_address = m_MA;
        int vsync_line_pos = ((m_6845_registers[MC6845_VSYNCPOS] & 0x7f) + 1) * character_line_count;
        int visible_line_count = (m_6845_registers[MC6845_VDISPLAYED] & 0x7f) * character_line_count;
        if (frame_buffer_line_index < m_image_height / 2)
        {
          if (m_scanline_index < (total_vertical_lines - vsync_line_pos) || m_scanline_index >= (total_vertical_lines - vsync_line_pos + visible_line_count))
          {
            // render borders (top, bottom)
            int frame_buffer_pos = frame_buffer_line_index * m_image_width * 2;
            uint color = BorderColorRegisterToColor(m_port_00h);
            uint dim_border_color = ConvertDimScanlineColor(color);

            for (int i = 0; i < m_image_width; i++)
            {
              m_frame_buffer.UIntBuffer[frame_buffer_pos] = color;
              m_frame_buffer.UIntBuffer[frame_buffer_pos+frame_buffer_line_length] = dim_border_color;
              frame_buffer_pos++;
            }
          }
          else
          {
            // draw border (left side)
            int frame_buffer_pos = frame_buffer_line_index * m_image_width * 2;
            int total_horizontal_pixel = (m_6845_registers[MC6845_HTOTAL] + 1) * 8;

            int first_visible_pixel = ((m_6845_registers[MC6845_HTOTAL] + 1) - (m_6845_registers[MC6845_HSYNCPOS] + 1)) * 8 - PixelHSyncPackPorch;
            if (first_visible_pixel < 0)
              first_visible_pixel = 0;

            uint border_color = BorderColorRegisterToColor(m_port_00h);
            uint dim_border_color = ConvertDimScanlineColor(border_color);

            int pixel_index = 0;
            while (pixel_index < first_visible_pixel)
            {
              m_frame_buffer.UIntBuffer[frame_buffer_pos] = border_color;
              m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_border_color;
              frame_buffer_pos++;
              pixel_index++;
            }

            int displayed_character_count = m_6845_registers[MC6845_HDISPLAYED];
            int character_index = 0;
            int video_memory_address;

            // draw pixels
            byte data;

            switch (m_port_06h)
            {
              // 2 color mode
              case 0:
                {
                  byte[] video_mem = m_tvc.Memory.VisibleVideoMem;
                  uint color0 = ColorRegisterToColor(m_port_palette[0]);
                  uint color1 = ColorRegisterToColor(m_port_palette[1]);
                  uint dim_color0 = ConvertDimScanlineColor(color0);
                  uint dim_color1 = ConvertDimScanlineColor(color1);

                  while (character_index < displayed_character_count)
                  {
                    video_memory_address = GenerateAndIncrementVideoMemoryAddress();

                    data = video_mem[video_memory_address];

                    // pixel 7
                    if ((data & 0x80) == 0)
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos+frame_buffer_line_length] = dim_color0;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
                    }
                    else
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos+frame_buffer_line_length] = dim_color1;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;
                    }

                    // pixel 6
                    if ((data & 0x40) == 0)
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color0;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
                    }
                    else
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color1;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;
                    }

                    // pixel 5
                    if ((data & 0x20) == 0)
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color0;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
                    }
                    else
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color1;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;
                    }

                    // pixel 4
                    if ((data & 0x10) == 0)
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color0;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
                    }
                    else
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color1;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;
                    }

                    // pixel 3
                    if ((data & 0x08) == 0)
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color0;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
                    }
                    else
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color1;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;
                    }

                    // pixel 2
                    if ((data & 0x04) == 0)
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color0;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
                    }
                    else
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color1;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;
                    }

                    // pixel 1
                    if ((data & 0x02) == 0)
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color0;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
                    }
                    else
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color1;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;
                    }

                    // pixel 0
                    if ((data & 0x01) == 0)
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color0;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
                    }
                    else
                    {
                      m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_color1;
                      m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;
                    }

                    character_index++;
                  }
                }
                break;

              // 4 color mode
              case 1:
                {
                  byte[] video_mem = m_tvc.Memory.VisibleVideoMem;

                  // get colors from palette
                  uint[] colors = new uint[32];
                  colors[0] = ColorRegisterToColor(m_port_palette[0]);
                  colors[1] = ColorRegisterToColor(m_port_palette[2]);
                  colors[16] = ColorRegisterToColor(m_port_palette[1]);
                  colors[17] = ColorRegisterToColor(m_port_palette[3]);

                  // dim colors
                  colors[0 + 2] = ConvertDimScanlineColor(colors[0]);
                  colors[1 + 2] = ConvertDimScanlineColor(colors[1]);
                  colors[16 + 2] = ConvertDimScanlineColor(colors[16]);
                  colors[17 + 2] = ConvertDimScanlineColor(colors[17]);

                  uint current_color;
                  uint current_dim_color;
                  int color_index;

                  while (character_index < displayed_character_count)
                  {
                    video_memory_address = GenerateAndIncrementVideoMemoryAddress();

                    data = video_mem[video_memory_address];

                    color_index = (data >> 3) & 0x11;
                    current_color = colors[color_index];
                    current_dim_color = colors[color_index + 2];

                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

                    color_index = (data >> 2) & 0x11;
                    current_color = colors[color_index];
                    current_dim_color = colors[color_index + 2];

                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

                    color_index = (data >> 1) & 0x11;
                    current_color = colors[color_index];
                    current_dim_color = colors[color_index + 2];

                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

                    color_index = data & 0x11;
                    current_color = colors[color_index];
                    current_dim_color = colors[color_index + 2];

                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

                    character_index++;
                  }
                }
                break;

              // 16 color mode
              default:
                {
                  byte[] video_mem = m_tvc.Memory.VisibleVideoMem;

                  // get colors from palette
                  uint current_color;
                  uint current_dim_color;
                  int color_index;

                  while (character_index < displayed_character_count)
                  {
                    video_memory_address = GenerateAndIncrementVideoMemoryAddress();

                    data = video_mem[video_memory_address];

                    color_index = (data >> 1) & 0x55;
                    current_color = m_current_graphics16_colors[color_index];
                    current_dim_color = m_current_graphics16_dim_colors[color_index];
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                      
                    color_index = data & 0x55;
                    current_color = m_current_graphics16_colors[color_index];
                    current_dim_color = m_current_graphics16_dim_colors[color_index];
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = current_dim_color;
                    m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

                    character_index++;
                  }
                }
                break;

            }

            // draw border (right side)
            pixel_index = first_visible_pixel + m_6845_registers[MC6845_HDISPLAYED] * 8; // ((m_6845_registers[MC6845_HSYNCPOS] + 1) - displayed_character_count) * 8;
            while (pixel_index < m_image_width)
            {
              m_frame_buffer.UIntBuffer[frame_buffer_pos] = border_color;
              m_frame_buffer.UIntBuffer[frame_buffer_pos + frame_buffer_line_length] = dim_border_color;
              frame_buffer_pos++;
              pixel_index++;
            }


            // handle cursor interrupt
            int cursor_address = (((m_6845_registers[MC6845_CURSORHI] << 8) + m_6845_registers[MC6845_CURSORLO]) & 0x3fff);
            int cursor_start_address = m_6845_registers[MC6845_CURSORSTART] & 0x1f;
            int marker_index = frame_buffer_line_index * m_image_width * 2;

            // if cursor is enabled (cursor IT is enabled)
            if ((m_6845_registers[MC6845_CURSORSTART] & 0x60) != 0x20)
            {
              // check address matching
              if (cursor_start_address == m_RA && line_start_video_memory_address <= cursor_address && (m_MA > cursor_address || m_MA < line_start_video_memory_address))
              {
                m_tvc.Interrupt.GenerateCursorSoundInterrupt();

                m_frame_buffer.UIntBuffer[marker_index] = 0xffffffff;
                m_frame_buffer.UIntBuffer[marker_index + 1] = 0xffffffff;
                m_frame_buffer.UIntBuffer[marker_index + 2] = 0xffffffff;
              }
              else
              {
                m_frame_buffer.UIntBuffer[marker_index] = 0xff000000;
                m_frame_buffer.UIntBuffer[marker_index + 1] = 0xff000000;
                m_frame_buffer.UIntBuffer[marker_index + 2] = 0xff000000;
              }
            }

            // next scanline
            m_RA++;

            if (m_RA >= 4)
            {
              m_RA = 0;
            }
            else
            {
              m_MA = line_start_video_memory_address;
            }
          }
        }
      }

      // next scanline
      m_scanline_index++;

      // end of frame
      if (m_scanline_index >= total_vertical_lines)
      {
        UpdateFrame();

        m_scanline_index = 0;
        m_frame_counter++;

        return true;
      }
      else
      {
        return false;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateAndIncrementVideoMemoryAddress()
    {
      int address = GenerateVideoMemoryAddress(m_MA, m_RA);

      m_MA = (m_MA + 1) & 0x3fff;

      return address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateVideoMemoryAddress(int in_ma, int in_ra)
    {
      return (in_ma & 0x3f) + ((in_ra & 0x03) << 6) + ((in_ma << 2) & 0x3f00);
    }

    private void FillColorCache()
    {
      // fill color cache
      int graphics16_color_index;
      int index;
      for (int i = 0; i < TVCColors.Length; i++)
      {
        if (BlackAndWhite)
          m_current_colors[i] = ConvertToBlackAndWhiteColor(TVCColors[i]);
        else
          m_current_colors[i] = 0xff000000u | ((uint)TVCColors[i].R << 16) | ((uint)TVCColors[i].G << 8) | (TVCColors[i].B);

        graphics16_color_index = 0;
        index = i;
        for (int j = 0; j < 4; j++)
        {
          graphics16_color_index <<= 2;

          if ((index & 0x08) != 0)
            graphics16_color_index |= 0x01;

          index <<= 1;
        }

        m_current_graphics16_colors[graphics16_color_index] = m_current_colors[i];
        m_current_graphics16_dim_colors[graphics16_color_index] = ConvertDimScanlineColor(m_current_colors[i]);
      }
    }

    private uint ColorRegisterToColor(byte in_register)
    {
      byte color_index = 0;

      if ((in_register & 0x40) != 0)
        color_index |= 0x08;

      if ((in_register & 0x10) != 0)
        color_index |= 0x04;

      if ((in_register & 0x04) != 0)
        color_index |= 0x02;

      if ((in_register & 0x01) != 0)
        color_index |= 0x01;

      Color color = TVCColors[color_index];

      if (BlackAndWhite)
        return ConvertToBlackAndWhiteColor(color);
      else
        return 0xff000000u | ((uint)color.R << 16) | ((uint)color.G << 8) | (color.B);
    }

    private uint BorderColorRegisterToColor(byte in_register)
    {
      byte color_index = 0;

      if ((in_register & 0x80) != 0)
        color_index |= 0x08;

      if ((in_register & 0x20) != 0)
        color_index |= 0x04;

      if ((in_register & 0x08) != 0)
        color_index |= 0x02;

      if ((in_register & 0x02) != 0)
        color_index |= 0x01;

      Color color = TVCColors[color_index];

      if (BlackAndWhite)
        return ConvertToBlackAndWhiteColor(color);
      else
        return 0xff000000u | ((uint)color.R << 16) | ((uint)color.G << 8) | (color.B);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint ConvertToBlackAndWhiteColor(Color inout_color)
    {
      byte Y = (byte)(255 * (0.229 * inout_color.R / 255.0 + 0.587 * inout_color.G / 255.0 + 0.114 * inout_color.B / 255.0));

      return 0xff000000u | ((uint)Y << 16) | ((uint)Y << 8) | (Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint ConvertDimScanlineColor(uint in_color)
    {
      uint ret_color = in_color & 0xff000000;

      ret_color |= DimScanlineColorMultiplier * (in_color & 0x000000ff) / 256;
      ret_color |= (DimScanlineColorMultiplier * ((in_color >> 8) & 0x000000ff) / 256) << 8;
      ret_color |= (DimScanlineColorMultiplier * ((in_color >> 16) & 0x000000ff) / 256) << 16;

      return ret_color;
    }
  }
}
