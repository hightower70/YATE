using System.Runtime.InteropServices;

namespace YATECommon.Files
{
  public class TVCFileTypes
  {
    public const int CASBlockHeaderFileBuffered = 0x01;
    public const int CASBlockHeaderFileUnbuffered = 0x11;

    // Tape/CAS Program file header
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CASProgramFileHeaderType
    {
      public byte Zero;               // Zero
      public byte FileType;           // Program type: 0x01 - ASCII, 0x00 - binary
      public ushort FileLength;       // Length of the file
      public byte Autorun;            // Autostart: 0xff, no autostart: 0x00
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
      public byte[] Zeros;            // Zero
      public byte Version;            // Version
    }

    // CAS UPM header
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CASUPMHeaderType
    {
      public byte FileType;       // File type: Buffered: 0x01, non-buffered: 0x11
      public byte CopyProtect;    // Copy Protect: 0x01    file is copy protected, 0x00 non protected
      public ushort BlockNumber;  // Number of the blocks (0x80 bytes length) occupied by the program
      public byte LastBlockBytes; // Number of the used bytes in the last block
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 123)]
      public byte[] Zeros;        // unused
    }

  }
}
