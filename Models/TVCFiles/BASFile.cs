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
// TV Computer BAS (Basic) File Loader-Saver
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVCEmu.Models.TVCFiles
{
	class BASFile
	{
		#region · Constants ·

		private const byte BAS_LINEND = 0xff; // line terminator
		private const byte BAS_PRGEND = 0x00; // program terminator

		private const byte BAS_TOKEN_DATA = 0xfb; // should not tokenize within DATA
		private const byte BAS_TOKEN_COMMENT = 0xfe; // should not tokenize after '!'
		private const byte BAS_TOKEN_REM = 0xfc; // should not tokenize after REM
		private const byte BAS_TOKEN_COLON = 0xfd; // should tokenize after ':' (if not in comment)

		private const int MAX_BYTES_IN_A_LINE = 16;

		#endregion

		public enum	EncodingType
		{
			Auto,
			Ansi,
			Unicode,
			Utf8
		};

		[Flags]
		private enum StatusCode
		{
			Tokenizing,		// normal tokenizing
			Quotation,		// within quotation marks
			Data,					// inside DATA
			Remark,				// inside remark line
			NonBasic			// non-basic (binary) part of the file
		};


		private struct BasicLineHeader
		{
			public byte Length;
			public ushort Number;

			public int ReadFromMemory(int in_pos, ProgramStorage in_storage)
			{
				Length = in_storage.Data[in_pos];
				in_pos += sizeof(byte);

				if (Length != BAS_LINEND && Length != BAS_PRGEND)
				{
					Number = BitConverter.ToUInt16(in_storage.Data, in_pos);
					in_pos += sizeof(ushort);
				}

				return in_pos;
			}

			public static int Size
			{
				get { return sizeof(byte) + sizeof(ushort); }
			}
		};

		public EncodingType Encoding { get; set; }


		///////////////////////////////////////////////////////////////////////////////
		// Saves BAS file
		public bool Save(Stream in_stream, ProgramStorage in_storage)
		{
			Encoding encoding;

			// set open options based on encoding type
			switch (Encoding)
			{
				case EncodingType.Ansi:
					encoding = new ASCIIEncoding();
					break;

				case EncodingType.Auto:
				case EncodingType.Utf8:
					encoding = new UTF8Encoding();
					break;

				case EncodingType.Unicode:
					encoding = new UnicodeEncoding();
					break;

				default:
					encoding = null;
					break;
			}


			using (StreamWriter bas_file = new StreamWriter(in_stream, encoding))
			{
				// start processing of the memory
				int current_pos = 0;
				int next_line_pos = 0;
				int line_data_end;
				StatusCode state = StatusCode.Tokenizing;

				BasicLineHeader current_line = new BasicLineHeader();
				current_line.ReadFromMemory(current_pos, in_storage);

				while (current_pos < in_storage.Length && current_line.Length != BAS_PRGEND)
				{
					// check basic format
					if (current_line.Length < BasicLineHeader.Size)
					{
						bas_file.Write("\n*** Broken BASIC program\n");
						break;
					}

					// set next line pointer
					next_line_pos = current_pos + current_line.Length;

					// write line number
					bas_file.Write("{0,4:d} ",current_line.Number);

					// decode  line
					current_pos += BasicLineHeader.Size;
					line_data_end = next_line_pos;
					if (current_pos <= line_data_end - 1 && in_storage.Data[line_data_end - 1] == BAS_LINEND)
						line_data_end--;

					state = StatusCode.Tokenizing;
					while (current_pos < line_data_end)
					{
						char current_char = (char)in_storage.Data[current_pos];

						// decode token or character
						if (state == StatusCode.Tokenizing)
						{
							// store tokenized item
							if (Encoding == EncodingType.Ansi)
							{
								bas_file.Write(m_ansi_tokenized_map[current_char]);
							}
							else
							{
								bas_file.Write(m_unicode_tokenized_map[current_char]);
							}
						}
						else
						{
							// store non tokenized item
							if (Encoding == EncodingType.Ansi)
							{
								if (current_char < 0x80)
									bas_file.Write(m_ansi_tokenized_map[current_char]);
								else
									bas_file.Write("\\x{0:X2}", current_char);
							}
							else
							{
								if (current_char < 0x80)
									bas_file.Write(m_unicode_tokenized_map[current_char]);
								else
									bas_file.Write("\\x{0:X2}", current_char);
							}
						}

						// update status
						if (current_char == '"')
						{
							state ^= StatusCode.Quotation;
						}
						else
						{
							if (!state.HasFlag(StatusCode.Quotation))
							{
								if (current_char == BAS_TOKEN_DATA)
								{
									state |= StatusCode.Data;
								}
								else
								{
									if (current_char == BAS_TOKEN_COLON)
									{
										state &= ~StatusCode.Data;
									}
									else
									{
										if (current_char == BAS_TOKEN_COMMENT || current_char == BAS_TOKEN_REM)
										{
											state |= StatusCode.Remark;
										}
									}
								}
							}
						}

						current_pos++;
					}

					bas_file.WriteLine();

					current_pos = next_line_pos;

					current_line.ReadFromMemory(current_pos, in_storage);
				}

				// write remaining data offset
				int remaining_byte_index = current_pos + 1; // +1 beacuse of the BAS_PRGEND byte
				if (remaining_byte_index < in_storage.Length)
				{
					bas_file.WriteLine("BYTESOFFSET {0}", remaining_byte_index);
				}

				// write remaining data
				int bytes_in_a_line = 0;
				while (remaining_byte_index < in_storage.Length)
				{
					if (bytes_in_a_line == 0)
					{
						bas_file.Write("BYTES ");
					}

					bas_file.Write("\\x{0:X2}", in_storage.Data[remaining_byte_index]);

					remaining_byte_index++;
					bytes_in_a_line++;

					// write new line
					if (bytes_in_a_line > MAX_BYTES_IN_A_LINE)
					{
						bas_file.WriteLine();
						bytes_in_a_line = 0;
					}
				}
				// new line
				if (bytes_in_a_line > 0)
				{
					bas_file.WriteLine();
				}

				// write autostart
				if (in_storage.AutoStart)
				{
					bas_file.WriteLine("AUTOSTART");
				}
			}

			return true;
		}

			private readonly string[] m_ansi_tokenized_map =
			{
// 			0		    1		    2			  3		    4       5       6       7		    8			   9       a         b        c        d				e        f
//	0x 'Á',    'É',    'Í',    'Ó',    'Ö',    'Ő',    'Ú',    'Ü',    'Ű',  '\x09', '\x0a',   '\x0b',  '\x0c',  '\x0d',  '\x0e',  '\x0f',
//  0x 'á',    'é',    'í',    'ó',    'ö',    'ő',    'ú',    'ü',    'ű',  '\x19', '\x1a',   '\x1b',  '\x1c',  '\x1d',  '\x1e',  '\x1f',
	  "\xc1", "\xc9", "\xcd", "\xd3", "\xd6", "\xd5", "\xda", "\xdc", "\xdb", "\\x09", "\\x0a", "\\x0b", "\\x0c", "\\x0d", "\\x0e", "\\x0f",
		"\xe1", "\xe9", "\xed", "\xf3", "\xf6", "\xf5", "\xfa", "\xfc", "\xfb", "\\x19", "\\x1a", "\\x1b", "\\x1c", "\\x1d", "\\x1e", "\\x1f",

		" ", "!", "\"", "#", "$", "%", "&", "'", "(", ")", "*", "+", ",",    "-", ".", "/",
		"0", "1", "2",  "3", "4", "5", "6", "7", "8", "9", ":", ";", "<",    "=", ">", "?",
		"@", "A", "B",  "C", "D", "E", "F", "G", "H", "I", "J", "K", "L",    "M", "N", "O",
		"P", "Q", "R",  "S", "T", "U", "V", "W", "X", "Y", "Z", "[", "\\\\", "]", "^", "_",
		"`", "a", "b",  "c", "d", "e", "f", "g", "h", "i", "j", "k", "l",    "m", "n", "o",
		"p", "q", "r",  "s", "t", "u", "v", "w", "x", "y", "z", "{", "|",    "}", "~", "\\x7f",

		"\\x80", "\\x81", "\\x82", "\\x83", "\\x84", "\\x85", "\\x86", "\\x87", "\\x88", "\\x89", "\\x8a", "\\x8b", "\\x8c", "\\x8d", "\\x8e", "\\x8f",

		"Cannot ",   "No ",       "Bad ",     "rgument",
		" missing",  ")",         "(",        "&",
		"+",         "<",         "=",        "<=",
		">",         "<>",        ">=",       "^",
		";",         "/",         "-",        "=<",
		",",         "><",        "=>",       "#",
		"*",         "TOKEN#A9",  "TOKEN#AA", "POLIGON",
		"RECTANGLE", "ELLIPSE",   "BORDER",   "USING",
		"AT",        "ATN",       "XOR",      "VOLUME",
		"TO",        "THEN",      "TAB",      "STYLE",
		"STEP",      "RATE",      "PROMPT",   "PITCH",
		"PAPER",     "PALETTE",   "PAINT",    "OR",
		"ORD",       "OFF",       "NOT",      "MODE",
		"INK",       "INKEY$",    "DURATION", "DELAY",
		"CHARACTER", "AND",       "TOKEN#CA", "TOKEN#CB",
		"EXCEPTION", "RENUMBER",  "FKEY",     "AUTO",
		"LPRINT",    "EXT",       "VERIFY",   "TRACE",
		"STOP",      "SOUND",     "SET",      "SAVE",
		"RUN",       "RETURN",    "RESTORE",  "READ",
		"RANDOMIZE", "PRINT",     "POKE",     "PLOT",
		"OUT",       "OUTPUT",    "OPEN",     "ON",
		"OK",        "NEXT",      "NEW",      "LOMEM",
		"LOAD",      "LLIST",     "LIST",     "LET",
		"INPUT",     "IF",        "GRAPHICS", "GOTO",
		"GOSUB",     "GET",       "FOR",      "END",
		"ELSE",      "DIM",       "DELETE",   "DEF",
		"CONTINUE",  "CLS",       "CLOSE",    "DATA",
		"REM",       ":",         "!",        "\\xff"
				};



		private readonly string[] m_unicode_tokenized_map =
		{
			// 			 0		      1		       2          3          4          5          6          7		       8         9         a         b         c         d         e        f
			//	0x  'Á',       'É',       'Í',       'Ó',       'Ö',       'Ő',       'Ú',       'Ü',       'Ű',   '\x09',   '\x0a',   '\x0b',   '\x0c',   '\x0d',   '\x0e',   '\x0f',
			//  0x  'á',       'é',       'í',       'ó',       'ö',       'ő',       'ú',       'ü',       'ű',   '\x19',   '\x1a',   '\x1b',   '\x1c',   '\x1d',   '\x1e',   '\x1f',
							"\x00c1", "\x00c9", "\x00cd", "\x00d3", "\x00d6", "\x0150", "\x00da", "\x00dc", "\x0170", "\\x09", "\\x0a", "\\x0b", "\\x0c", "\\x0d", "\\x0e", "\\x0f",
							"\x00e1", "\x00e9", "\x00ed", "\x00f3", "\x00f6", "\x0151", "\x00fa", "\x00fc", "\x0171", "\\x19", "\\x1a", "\\x1b", "\\x1c", "\\x1d", "\\x1e", "\\x1f",

			" ", "!", "\"", "#", "$", "%", "&", "'", "(", ")", "*", "+", ",",    "-", ".", "/",
			"0", "1", "2",  "3", "4", "5", "6", "7", "8", "9", ":", ";", "<",    "=", ">", "?",
			"@", "A", "B",  "C", "D", "E", "F", "G", "H", "I", "J", "K", "L",    "M", "N", "O",
			"P", "Q", "R",  "S", "T", "U", "V", "W", "X", "Y", "Z", "[", "\\\\", "]", "^", "_",
			"`", "a", "b",  "c", "d", "e", "f", "g", "h", "i", "j", "k", "l",    "m", "n", "o",
			"p", "q", "r",  "s", "t", "u", "v", "w", "x", "y", "z", "{", "|",    "}", "~", "\\x7f",

			"\\x80", "\\x81", "\\x82", "\\x83", "\\x84", "\\x85", "\\x86", "\\x87", "\\x88", "\\x89", "\\x8a", "\\x8b", "\\x8c", "\\x8d", "\\x8e", "\\x8f",

			"Cannot ",   "No ",       "Bad ",     "rgument",
			" missing",  ")",         "(",        "&",
			"+",         "<",         "=",        "<=",
			">",         "<>",        ">=",       "^",
			";",         "/",         "-",        "=<",
			",",         "><",        "=>",       "#",
			"*",         "TOKEN#A9",  "TOKEN#AA", "POLIGON",
			"RECTANGLE", "ELLIPSE",   "BORDER",   "USING",
			"AT",         "ATN",       "XOR",      "VOLUME",
			"TO",        "THEN",      "TAB",      "STYLE",
			"STEP",      "RATE",      "PROMPT",   "PITCH",
			"PAPER",     "PALETTE",   "PAINT",    "OR",
			"ORD",       "OFF",       "NOT",      "MODE",
			"INK",       "INKEY$",    "DURATION", "DELAY",
			"CHARACTER", "AND",       "TOKEN#CA", "TOKEN#CB",
			"EXCEPTION", "RENUMBER",  "FKEY",     "AUTO",
			"LPRINT",    "EXT",       "VERIFY",   "TRACE",
			"STOP",      "SOUND",     "SET",      "SAVE",
			"RUN",       "RETURN",    "RESTORE",  "READ",
			"RANDOMIZE", "PRINT",     "POKE",     "PLOT",
			"OUT",       "OUTPUT",    "OPEN",     "ON",
			"OK",        "NEXT",      "NEW",      "LOMEM",
			"LOAD",      "LLIST",     "LIST",     "LET",
			"INPUT",     "IF",        "GRAPHICS", "GOTO",
			"GOSUB",     "GET",       "FOR",      "END",
			"ELSE",      "DIM",       "DELETE",   "DEF",
			"CONTINUE",  "CLS",       "CLOSE",    "DATA",
			"REM",       ":",         "!",        "\\xff"
		};


	}
}
