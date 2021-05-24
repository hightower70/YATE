using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SDCart
{
  class FISH
  {
    // Define GetShortPathName API function.
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern uint GetShortPathName(string lpszLongPath,
    char[] lpszShortPath, int cchBuffer);


    // Return the short file name for a long file name.
    private string ShortFileName(string long_name)
    {
      char[] name_chars = new char[1024];
      long length = GetShortPathName(
          long_name, name_chars,
          name_chars.Length);

      string short_name = new string(name_chars);
      return short_name.Substring(0, (int)length);
    }

    // Return the long file name for a short file name.
    private string LongFileName(string short_name)
    {
      return new FileInfo(short_name).FullName;
    }
  }
}
