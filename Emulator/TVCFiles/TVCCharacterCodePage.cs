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
// TVC character map conversion toutines to various other character maps
///////////////////////////////////////////////////////////////////////////////

namespace YATE.Emulator.TVCFiles
{
  class TVCCharacterCodePage
	{
		public const int CHARACTER_NUMBER = 18;

		// Unicode to TVC lookup table
		public class UnicodeToTVCCharMap
		{
			public char UChar;
			public char TVCChar;

			public UnicodeToTVCCharMap(char in_unicode_char, byte in_tvc_char)
			{
				UChar = in_unicode_char;
				TVCChar = (char)in_tvc_char;
			}
		}

		/*****************************************************************************/
		/* ANSI <-> TVC conversion                                                   */
		/*****************************************************************************/

		///////////////////////////////////////////////////////////////////////////////
		// TVC character to ANSI character conversion
		public static char TVCCharToANSIIChar(char in_tvc)
		{
			if ((byte)in_tvc < 128)
			{
				return in_tvc;
			}
			else
			{
				return l_tvc_to_ansi[(byte)in_tvc - 128];
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// TVC string to ANSI string conversion (inplace conversion is supported too)
		public static string TVCStringToANSIString(string in_tvc_string)
		{
			string ansi_string = "";

			for (int i = 0; i < in_tvc_string.Length; i++)
				ansi_string += TVCCharToANSIIChar(in_tvc_string[i]);

			return ansi_string.ToString();
		}

		///////////////////////////////////////////////////////////////////////////////
		// ANSI character to TVC character conversion
		public static char ANSIICharToTVCChar(char in_ansii)
		{
			if ((byte)in_ansii < 128)
			{
				return in_ansii;
			}
			else
			{
				return l_ansi_to_tvc[(byte)in_ansii - 128];
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// ANSI string to TVC string
		public static string ANSIStringToTVCString(string in_ansi_string)
		{
			string tvc_string = "";

			for (int i = 0; i < in_ansi_string.Length; i++)
				tvc_string += TVCCharToANSIIChar(in_ansi_string[i]);

			return tvc_string.ToString();
		}

		///////////////////////////////////////////////////////////////////////////////
		// TVC character to ANSI character conversion
		public static char TVCCharToUNICODEChar(char in_tvc)
		{
			if ((byte)in_tvc < 128)
			{
				return in_tvc;
			}
			else
			{
				return l_tvc_to_unicode[(byte)in_tvc - 128];
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// TVC string to ANSI string conversion (inplace conversion is supported too)
		public static string TVCStringToUNICODEString(string in_tvc_string)
		{
			string unicode_string = "";

			for (int i = 0; i < in_tvc_string.Length; i++)
				unicode_string += TVCCharToUNICODEChar(in_tvc_string[i]);

			return unicode_string;
		}

		///////////////////////////////////////////////////////////////////////////////
		// Converts Unicode to TVC Character
		public static char UNICODECharToTVCChar(char in_char)
		{
			int first;
			int last;
			int middle;
			if (in_char < l_unicode_to_tvc[0].UChar)
			{
				return (char)in_char;
			}

			first = 0;
			last = CHARACTER_NUMBER - 1;
			while (first <= last)
			{
				middle = (first + last) / 2;
				if (l_unicode_to_tvc[middle].UChar < in_char)
				{
					first = middle + 1;
				}
				else
				{
					if (l_unicode_to_tvc[middle].UChar == in_char)
					{
						return l_unicode_to_tvc[middle].TVCChar;
					}
					else
					{
						last = middle - 1;
					}
				}
			}

			return '\0';
		}

		///////////////////////////////////////////////////////////////////////////////
		// Unicode string to TVC string conversion
		public static string UNICODEStringToTVCString(string in_unicode_string)
		{
			string tvc_string = "";

			for (int i = 0; i < in_unicode_string.Length; i++)
				tvc_string += UNICODECharToTVCChar(in_unicode_string[i]);

			return tvc_string;
		}

		/*****************************************************************************/
		/* ASCII codepage                                                            */
		/*****************************************************************************/

		///////////////////////////////////////////////////////////////////////////////
		// TVC character to ASCII character conversion
		public static char TVCCharToASCIIChar(char in_tvc)
		{
			if ((byte)in_tvc < 128)
			{
				return in_tvc;
			}
			else
			{
				return l_tvc_to_ascii[(byte)in_tvc - 128];
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// TVC string to ASCII string conversion (inplace conversion is supported too)
		public static string TVCStringToASCIIString(string in_tvc_string)
		{
			string ascii_string = "";

			for (int i = 0; i < in_tvc_string.Length; i++)
				ascii_string += TVCCharToASCIIChar(in_tvc_string[i]);

			return ascii_string;
		}

		/*****************************************************************************/
		/* Conversion tables                                                         */
		/*****************************************************************************/


		// TVC Character encoding
		// 			0		 1		2			3		 4		5		 6		7		 8			9				a				b				c				d				e				f
		//	8x 'Á', 'É', 'Í',  'Ó', 'Ö', 'Ő', 'Ú', 'Ü', 'Ű', '\x89', '\x8a', '\x8b', '\x8c', '\x8d', '\x8e', '\x8f',
		//  9x 'á', 'é', 'í',  'ó', 'ö', 'ő', 'ú', 'ü', 'ű', '\x99', '\x9a', '\x9b', '\x9c', '\x9d', '\x9e', '\x9f',

		internal static char[] l_tvc_to_ansi = { '\xc1', '\xc9', '\xcd', '\xd3', '\xd6', '\xd5', '\xda', '\xdc', '\xdb', '\x89', '\x8a', '\x8b', '\x8c', '\x8d', '\x8e', '\x8f', '\xe1', '\xe9', '\xed', '\xf3', '\xf6', '\xf5', '\xfa', '\xfc', '\xfb', '\x99', '\x9a', '\x9b', '\x9c', '\x9d', '\x9e', '\x9f', '\xa0', '\xa1', '\xa2', '\xa3', '\xa4', '\xa5', '\xa6', '\xa7', '\xa8', '\xa9', '\xaa', '\xab', '\xac', '\xad', '\xae', '\xaf', '\xb0', '\xb1', '\xb2', '\xb3', '\xb4', '\xb5', '\xb6', '\xb7', '\xb8', '\xb9', '\xba', '\xbb', '\xbc', '\xbd', '\xbe', '\xbf', '\xc0', '\xc1', '\xc2', '\xc3', '\xc4', '\xc5', '\xc6', '\xc7', '\xc8', '\xc9', '\xca', '\xcb', '\xcc', '\xcd', '\xce', '\xcf', '\xd0', '\xd1', '\xd2', '\xd3', '\xd4', '\xd5', '\xd6', '\xd7', '\xd8', '\xd9', '\xda', '\xdb', '\xdc', '\xdd', '\xde', '\xdf', '\xe0', '\xe1', '\xe2', '\xe3', '\xe4', '\xe5', '\xe6', '\xe7', '\xe8', '\xe9', '\xea', '\xeb', '\xec', '\xed', '\xee', '\xef', '\xf0', '\xf1', '\xf2', '\xf3', '\xf4', '\xf5', '\xf6', '\xf7', '\xf8', '\xf9', '\xfa', '\xfb', '\xfc', '\xfd', '\xfe', '\xff' };

		internal static char[] l_ansi_to_tvc = { '\x80', '\x81', '\x82', '\x83', '\x84', '\x85', '\x86', '\x87', '\x88', '\x89', '\x8a', '\x8b', '\x8c', '\x8d', '\x8e', '\x8f', '\x90', '\x91', '\x92', '\x93', '\x94', '\x95', '\x96', '\x97', '\x98', '\x99', '\x9a', '\x9b', '\x9c', '\x9d', '\x9e', '\x9f', '\xa0', '\xa1', '\xa2', '\xa3', '\xa4', '\xa5', '\xa6', '\xa7', '\xa8', '\xa9', '\xaa', '\xab', '\xac', '\xad', '\xae', '\xaf', '\xb0', '\xb1', '\xb2', '\xb3', '\xb4', '\xb5', '\xb6', '\xb7', '\xb8', '\xb9', '\xba', '\xbb', '\xbc', '\xbd', '\xbe', '\xbf', '\xc0', '\x80', '\xc2', '\xc3', '\xc4', '\xc5', '\xc6', '\xc7', '\xc8', '\x81', '\xca', '\xcb', '\xcc', '\x82', '\xce', '\xcf', '\xd0', '\xd1', '\xd2', '\x83', '\xd4', '\x85', '\x84', '\xd7', '\xd8', '\xd9', '\x86', '\x88', '\x87', '\xdd', '\xde', '\xdf', '\xe0', '\x90', '\xe2', '\xe3', '\xe4', '\xe5', '\xe6', '\xe7', '\xe8', '\x91', '\xea', '\xeb', '\xec', '\x92', '\xee', '\xef', '\xf0', '\xf1', '\xf2', '\x93', '\xf4', '\x95', '\x94', '\xf7', '\xf8', '\xf9', '\x96', '\x98', '\x97', '\xfd', '\xfe', '\xff' };


		internal static char[] l_tvc_to_ascii = { 'A', 'E', 'I', 'O', 'O', 'O', 'U', 'U', 'U', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'a', 'e', 'i', 'o', 'o', 'o', 'u', 'u', 'u', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' };

		///////////////////////////////////////////////////////////////////////////////
		// UNICODE codepage
		internal static char[] l_tvc_to_unicode = { '\x00c1', '\x00c9', '\x00cd', '\x00d3', '\x00d6', '\x0150', '\x00da', '\x00dc', '\x0170', '\x0089', '\x008a', '\x008b', '\x008c', '\x008d', '\x008e', '\x008f', '\x00e1', '\x00e9', '\x00ed', '\x00f3', '\x00f6', '\x0151', '\x00fa', '\x00fc', '\x0171', '\x0099', '\x009a', '\x009b', '\x009c', '\x009d', '\x009e', '\x009f', '\x00a0', '\x00a1', '\x00a2', '\x00a3', '\x00a4', '\x00a5', '\x00a6', '\x00a7', '\x00a8', '\x00a9', '\x00aa', '\x00ab', '\x00ac', '\x00ad', '\x00ae', '\x00af', '\x00b0', '\x00b1', '\x00b2', '\x00b3', '\x00b4', '\x00b5', '\x00b6', '\x00b7', '\x00b8', '\x00b9', '\x00ba', '\x00bb', '\x00bc', '\x00bd', '\x00be', '\x00bf', '\x00c0', '\x00c1', '\x00c2', '\x00c3', '\x00c4', '\x00c5', '\x00c6', '\x00c7', '\x00c8', '\x00c9', '\x00ca', '\x00cb', '\x00cc', '\x00cd', '\x00ce', '\x00cf', '\x00d0', '\x00d1', '\x00d2', '\x00d3', '\x00d4', '\x00d5', '\x00d6', '\x00d7', '\x00d8', '\x00d9', '\x00da', '\x00db', '\x00dc', '\x00dd', '\x00de', '\x00df', '\x00e0', '\x00e1', '\x00e2', '\x00e3', '\x00e4', '\x00e5', '\x00e6', '\x00e7', '\x00e8', '\x00e9', '\x00ea', '\x00eb', '\x00ec', '\x00ed', '\x00ee', '\x00ef', '\x00f0', '\x00f1', '\x00f2', '\x00f3', '\x00f4', '\x00f5', '\x00f6', '\x00f7', '\x00f8', '\x00f9', '\x00fa', '\x00fb', '\x00fc', '\x00fd', '\x00fe', '\x00ff' };

		internal static UnicodeToTVCCharMap[] l_unicode_to_tvc =
		{
			new UnicodeToTVCCharMap('\x00c1', 0x80),
			new UnicodeToTVCCharMap('\x00c9', 0x81),
			new UnicodeToTVCCharMap('\x00cd', 0x82),
			new UnicodeToTVCCharMap('\x00d3', 0x83),
			new UnicodeToTVCCharMap('\x00d6', 0x84),
			new UnicodeToTVCCharMap('\x00da', 0x86),
			new UnicodeToTVCCharMap('\x00dc', 0x87),
			new UnicodeToTVCCharMap('\x00e1', 0x90),
			new UnicodeToTVCCharMap('\x00e9', 0x91),
			new UnicodeToTVCCharMap('\x00ed', 0x92),
			new UnicodeToTVCCharMap('\x00f3', 0x93),
			new UnicodeToTVCCharMap('\x00f6', 0x94),
			new UnicodeToTVCCharMap('\x00fa', 0x96),
			new UnicodeToTVCCharMap('\x00fc', 0x97),
			new UnicodeToTVCCharMap('\x0150', 0x85),
			new UnicodeToTVCCharMap('\x0151', 0x95),
			new UnicodeToTVCCharMap('\x0170', 0x88),
			new UnicodeToTVCCharMap('\x0171', 0x98)
		};
	}
}
