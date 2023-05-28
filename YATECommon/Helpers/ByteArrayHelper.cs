///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2023 Laszlo Arvai. All rights reserved.
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
// Extension methods for byte array handling
///////////////////////////////////////////////////////////////////////////////

namespace YATECommon.Helpers
{
  public static class ByteArrayHelper
  {
    /// <summary>
    /// Converts byte array bytes to ushort
    /// </summary>
    /// <param name="in_array">Bytes to convert</param>
    /// <param name="offset">Offset within the byte array of the data to be converted</param>
    /// <returns></returns>
    public static ushort ToUshort(this byte[] in_array, int offset)
    {
      return (ushort)(in_array[offset] + (in_array[offset + 1] << 8));
    }

    /// <summary>
    /// Convert ushort to bytes
    /// </summary>
    /// <param name="in_array">Byte array to store bytes</param>
    /// <param name="in_offset">Offset of the byte position to store</param>
    /// <param name="in_value">Value to be converted</param>
    public static void FromUshort(this byte[] in_array, int in_offset, ushort in_value )
    {
      in_array[in_offset] = (byte)(in_value & 0xff);
      in_array[in_offset] = (byte)(in_value >> 8);
    }
  }
}
