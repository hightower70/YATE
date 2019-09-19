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
// Videoton TV Computer Video Hardware Emulation
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;

namespace TVCHardware
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

		public const int HorizontalTiming = 64000; // horizontal timing in ns

		public readonly Color[] TVCColors =
		{
			Color.FromArgb(255, 0, 0, 0),				// Black
			Color.FromArgb(255, 0, 0, 128),			// DarkBlue
			Color.FromArgb(255, 128, 0, 0),			// DarkRed
			Color.FromArgb(255, 128, 0, 128),		// DarkPurple
			Color.FromArgb(255, 0, 128, 0),			// DarkGreen
			Color.FromArgb(255, 0, 128, 128),		// DarkCyan
			Color.FromArgb(255, 128, 128, 0),		// DarkYellow
			Color.FromArgb(255, 128, 128, 128), // DarkGray

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

		// video refresh variables
		private uint[] m_colors;
		private uint[] m_graphics16_colors;

		private int m_scanline_index = 0;
		private int m_RA = 0;
		private int m_MA = 0;

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

			m_context = SynchronizationContext.Current;

			// fill color cache
			m_graphics16_colors = new uint[128];
			int graphics16_color_index;
			int index;
			m_colors = new uint[TVCColors.Length];
			for (int i = 0; i < TVCColors.Length; i++)
			{
				m_colors[i] = 0xff000000u | ((uint)TVCColors[i].R << 16) | ((uint)TVCColors[i].G << 8) | (TVCColors[i].B);

				graphics16_color_index = 0;
				index = i;
				for (int j = 0; j < 4; j++)
				{
					graphics16_color_index <<= 2;

					if ((index & 0x08) != 0)
						graphics16_color_index |= 0x01;

					index <<= 1;
				}

				m_graphics16_colors[graphics16_color_index] = m_colors[i];
			}

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

			if(m_6845_address==5)
			{

			}
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
			int total_character_count = m_6845_registers[MC6845_HTOTAL] + 1;

			if(total_character_count != 100)
			{
				//TODO: video error
				return false;
			}

			int character_line_count = (m_6845_registers[MC6845_MAXSCANLINEADDR] & 0x1f) + 1;
			if(character_line_count != 4)
			{
				//TODO: video error
				return false;
			}

			// calculate total lines
			int total_vertical_lines = ((m_6845_registers[MC6845_VTOTAL] & 0x7f) + 1) * character_line_count + (m_6845_registers[MC6845_VTOTALADJ] & 0x1f);
			if (total_vertical_lines < 310 || total_vertical_lines > 320)
			{
				//TODO: video error
				return false;
			}

			// determine first visible line
			int first_visible_line = 25;

			if (m_scanline_index < first_visible_line)
			{
				m_scanline_index++;
				return false;
			}


			// start rendering
			if(m_scanline_index == first_visible_line)
			{
				m_RA = 0;
				m_MA = ((m_6845_registers[MC6845_STARTADDRHI] & 0x3f) << 8) + m_6845_registers[MC6845_STARTADDRLO];
			}

			// check for display enabled in vertical direction
			int vsync_line_pos = ((m_6845_registers[MC6845_VSYNCPOS] & 0x7f) + 1) * character_line_count;
			int visible_line_count = (m_6845_registers[MC6845_VDISPLAYED] & 0x7f) * character_line_count;
			if ( m_scanline_index < (total_vertical_lines - vsync_line_pos) || m_scanline_index >= (total_vertical_lines - vsync_line_pos + visible_line_count))
			{
				if ((m_scanline_index - first_visible_line) < m_image_height / 2)
				{
					// render borders (top, bottom)
					int frame_buffer_pos = (m_scanline_index - first_visible_line) * m_image_width * 2;
					uint color = BorderColorRegisterToColor(m_port_00h);

					for (int i = 0; i < m_image_width; i++)
					{
						m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color;
					}
				}
			}
			else
			{
				// draw border (left side)
				int first_visible_column = 140;
				int total_horizontal_pixel = (m_6845_registers[MC6845_HTOTAL] + 1) * 8;
				int image_offset = (total_horizontal_pixel - first_visible_column - m_image_width) / 2;
				int frame_buffer_pos = (m_scanline_index - first_visible_line) * m_image_width * 2 -first_visible_column;
				int pixel_index = 0;
				int first_visible_pixel = ((m_6845_registers[MC6845_HTOTAL] + 1) - (m_6845_registers[MC6845_HSYNCPOS] + 1)) * 8 + image_offset;
				uint border_color = BorderColorRegisterToColor(m_port_00h);
				while (pixel_index < first_visible_pixel)
				{
					m_frame_buffer.UIntBuffer[frame_buffer_pos++] = border_color;
					pixel_index++;
				}

				int displayed_character_count = m_6845_registers[MC6845_HDISPLAYED];
				int character_index = 0;
				int video_memory_address;
				int line_start_video_memory_address = m_MA;

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

							while (character_index < displayed_character_count)
							{
								video_memory_address = GenerateAndIncrementVideoMemoryAddress();

								data = video_mem[video_memory_address];

								// pixel 7
								if ((data & 0x80) == 0)
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
								else
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;

								// pixel 6
								if ((data & 0x40) == 0)
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
								else
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;

								// pixel 5
								if ((data & 0x20) == 0)
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
								else
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;

								// pixel 4
								if ((data & 0x10) == 0)
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
								else
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;

								// pixel 3
								if ((data & 0x08) == 0)
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
								else
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;

								// pixel 2
								if ((data & 0x04) == 0)
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
								else
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;

								// pixel 1
								if ((data & 0x02) == 0)
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
								else
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;

								// pixel 0
								if ((data & 0x01) == 0)
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color0;
								else
									m_frame_buffer.UIntBuffer[frame_buffer_pos++] = color1;

								character_index++;
							}
						}
						break;

					case 1:
						{
							byte[] video_mem = m_tvc.Memory.VisibleVideoMem;

							// get colors from palette
							uint[] colors = new uint[32];
							colors[0] = ColorRegisterToColor(m_port_palette[0]);
							colors[1] = ColorRegisterToColor(m_port_palette[2]);
							colors[16] = ColorRegisterToColor(m_port_palette[1]);
							colors[17] = ColorRegisterToColor(m_port_palette[3]);

							uint current_color;

							while (character_index < displayed_character_count)
							{
								video_memory_address = GenerateAndIncrementVideoMemoryAddress();

								data = video_mem[video_memory_address];

								current_color = colors[(data >> 3) & 0x11];
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

								current_color = colors[(data >> 2) & 0x11];
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

								current_color = colors[(data >> 1) & 0x11];
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

								current_color = colors[data & 0x11];
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

								character_index++;
							}
						}
						break;

					default:
						{
							byte[] video_mem = m_tvc.Memory.VisibleVideoMem;

							// get colors from palette
							uint current_color;

							while (character_index < displayed_character_count)
							{
								video_memory_address = GenerateAndIncrementVideoMemoryAddress();

								data = video_mem[video_memory_address];

								current_color = m_graphics16_colors[(data >> 1) & 0x55];
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

								current_color = m_graphics16_colors[data & 0x55];
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;
								m_frame_buffer.UIntBuffer[frame_buffer_pos++] = current_color;

								character_index++;
							}
						}
						break;

				}

				// draw border (right side)
				int right_border_pixel_count = ((m_6845_registers[MC6845_HSYNCPOS] + 1) - displayed_character_count) * 8;
				pixel_index = 0;
				while (pixel_index < right_border_pixel_count)
				{
					m_frame_buffer.UIntBuffer[frame_buffer_pos++] = border_color;
					pixel_index++;
				}

				// handle cursor interrupt
				int cursor_address = ((m_6845_registers[MC6845_CURSORHI] << 8) + m_6845_registers[MC6845_CURSORLO]);
				int cursor_start_address = m_6845_registers[MC6845_CURSORSTART] & 0x1f;
				int marker_index = (m_scanline_index - first_visible_line) * m_image_width * 2;

				// if cursor is enabled (cursor IT is enabled)
				if ((m_6845_registers[MC6845_CURSORSTART] & 60) == 0)
				{
					// check address matching
					if (cursor_start_address == m_RA && line_start_video_memory_address <= cursor_address && (m_MA >= cursor_address || m_MA < line_start_video_memory_address))
					{
						m_tvc.Interrupt.GenerateCursorSoundInterrupt();

						m_frame_buffer.UIntBuffer[marker_index ] = 0xffffffff;
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

				if(m_RA>=4)
				{
					m_RA = 0;
				}
				else
				{
					m_MA = line_start_video_memory_address;
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
			return (in_ma & 0x3f) + (in_ra << 6) + ((in_ma << 2) & 0x3f00);
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

			return 0xff000000u | ((uint)color.R << 16) | ((uint)color.G << 8) | (color.B);
		}
	}
}
