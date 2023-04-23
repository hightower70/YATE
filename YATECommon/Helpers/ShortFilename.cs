using System.IO;
using System.Runtime.InteropServices;
///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2013-2023 Laszlo Arvai. All rights reserved.
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
// Short (8.3) filename helper functions
///////////////////////////////////////////////////////////////////////////////
namespace YATECommon.Helpers
{
  public class ShortFilename
  {
    // Define GetShortPathName API function.
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern uint GetShortPathName(string lpszLongPath, char[] lpszShortPath, int cchBuffer);

    /// <summary>
    /// Return the short file name for a long file name.
    /// </summary>
    /// <param name="long_name">Long file name</param>
    /// <returns>Short file name</returns>
    public static string ShortFileName(string long_name)
    {
      char[] name_chars = new char[1024];
      long length = GetShortPathName(long_name, name_chars, name_chars.Length);

      string short_name = new string(name_chars);
      return short_name.Substring(0, (int)length);
    }

    /// <summary>
    /// Return the long file name for a short file name.
    /// </summary>
    /// <param name="short_name">Short file name</param>
    /// <returns>Long file name</returns>
    public static string LongFileName(string short_name)
    {
      return new FileInfo(short_name).FullName;
    }
  }
}
