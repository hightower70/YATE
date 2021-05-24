using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDCart
{
  class CoProcessorConstants
  {
    public const int FunctionGroupCount = 16;

    //public const int MaxFileNameLength = 16;

    public const byte CPFN_OPEN = 0x01;
    public const byte CPFN_RDCH = 0x02;
    public const byte CPFN_RDBLK = 0x03;
    public const byte CPFN_RDCLOSE = 0x04;
    public const byte CPFN_CREATE = 0x05;
    public const byte CPFN_WRCH = 0x06;
    public const byte CPFN_WRBLK = 0x07;
    public const byte CPFN_WRCLOSE = 0x08;
    public const byte CPFN_VYBLK = 0x09;


    public const byte ERR_OK = 0x00;
    public const byte ERR_BUSY = 0x11;
    public const byte ERR_RESULT = 0x12;
    public const byte ERR_FILE_EXISTS = 0x9b;
    public const byte ERR_NO_PATH = 0xa0;
    public const byte ERR_FILE_NOT_FOUND = 0xa1;
    public const byte ERR_DISK_FULL = 0xa3;
    public const byte ERR_NO_MORE_DATA = 0xe7;
    public const byte ERR_VERIFY = 0xe8;
    public const byte ERR_NO_OPENED_FILE = 0xe9;
    public const byte ERR_ALREADY_OPENED = 0xeb;
    public const byte ERR_END_OF_FILE = 0xec;
    public const byte ERR_UNKNOWN_FUNCTION = 0xff;
  }
}
