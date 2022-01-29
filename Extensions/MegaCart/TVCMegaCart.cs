using System;
using System.IO;
using YATECommon;
using YATECommon.Helpers;

namespace MegaCart
{
  public class TVCMegaCart : ITVCCartridge
  {
    private MegaCartSettings m_settings;

    public const int MaxCartRomSize = 1024 * 1024;
    private const UInt16 PAGE_REGISTER_ADDRESS_MASK = 0x3fff;
    private const UInt16 PAGE_REGISTER_MIN_ADDRESS = 0x3c00;
    private const UInt16 PAGE_REGISTER_MAX_ADDRESS = 0x3fff;
    private const byte PAGE_REGISTER_MASK = 0x3f;

    public byte[] Rom { get; private set; }

    private byte m_page_register = 0;

    public bool SetSettings(MegaCartSettings in_settings)
    {
      m_settings = in_settings;

      // load ROM
      Rom = new byte[MaxCartRomSize];
      for (int i = 0; i < Rom.Length; i++)
        Rom[i] = 0xff;

      bool settings_changed = LoadROMContent(m_settings.ROMFileName, 0);

      // initialize members
      m_page_register = 0;

      return settings_changed;
    }

    private bool LoadROMContent(string in_rom_file_name, int in_address)
    {
      bool changed = false;
      byte[] old_rom = null;

      // save old rom
      old_rom = Rom;

      ROMFile.LoadMemoryFromFile(in_rom_file_name, Rom);

      changed = !ROMFile.IsMemoryEqual(old_rom, Rom);

      return changed;
    }

    public byte MemoryRead(ushort in_address)
    {
      return Rom[in_address + (m_page_register << 14)];
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      if((in_address & PAGE_REGISTER_ADDRESS_MASK) >= PAGE_REGISTER_MIN_ADDRESS)
      {
        m_page_register = (byte)(in_byte & PAGE_REGISTER_MASK);
      }
    }

    public void Reset()
    {
      m_page_register = 0;
    }

    public void Initialize(ITVComputer in_parent)
    {
    }

    public void Remove(ITVComputer in_parent)
    {
    }
  }
}
