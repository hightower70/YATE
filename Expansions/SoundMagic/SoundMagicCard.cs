using System;
using System.IO;
using System.Reflection;
using YATECommon;

namespace SoundMagic
{
  public class SoundMagicCard : ITVCCard
  {
    public const int RomSize = 32 * 1024;   // 32k is the max ROM size

    private ITVComputer m_tvcomputer;
    private SoundMagicSettings m_settings;

    private byte m_page_register = 0xff;
    private byte[] m_card_rom;

    public SoundMagicCard() 
    {
    }

    public void SetSettings(SoundMagicSettings in_settings)
    {
      m_settings = in_settings;

      // load ROM
      m_card_rom = new byte[RomSize];
      for (int i = 0; i < m_card_rom.Length; i++)
        m_card_rom[i] = 0xff;

      LoadCardRomFromResource("SoundMagic.Resources.sndmx-fw-v1.0.1.bin");
    }

    private void LoadROMContent(string in_rom_file_name)
    {
      if (!string.IsNullOrEmpty(in_rom_file_name))
      {
        byte[] data = File.ReadAllBytes(in_rom_file_name);

        int length = data.Length;

        if (data.Length > m_card_rom.Length)
          length = m_card_rom.Length;

        Array.Copy(data, 0, m_card_rom, 0, length);
      }
    }

    /// <summary>
    /// Loads card ROM content from the given resource file
    /// </summary>
    /// <param name="in_resource_name"></param>
    public void LoadCardRomFromResource(string in_resource_name)
    {
      // load default key mapping
      Assembly assembly = Assembly.GetExecutingAssembly();

      using (Stream stream = assembly.GetManifestResourceStream(in_resource_name))
      {
        using (BinaryReader binary_reader = new BinaryReader(stream))
        {
          byte[] data = binary_reader.ReadBytes((int)stream.Length);

          int byte_to_copy = data.Length;

          if (byte_to_copy > m_card_rom.Length)
            byte_to_copy = m_card_rom.Length;

          Array.Copy(data, 0, m_card_rom, 0, byte_to_copy);
        }
      }
    }

    public byte MemoryRead(ushort in_address)
    {
      int address = (in_address & 0x1fff) + ((m_page_register & 0x03) << 13);

      if (address < m_card_rom.Length)
        return m_card_rom[address];
      else
        return 0xff;
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // no memory write
    }

    public void Reset()
    {
      m_page_register = 0xff;
    }

    public void PortRead(ushort in_address, ref byte inout_data)
    {
      inout_data = 0xff; // no port read
    }

    public void PortWrite(ushort in_address, byte in_byte)
    {
      switch((in_address >> 1) & 0x03)
      {
        case 0:
          break;

        case 1:
          break;

        case 2:
          break;

        case 3:
          m_page_register = in_byte;
          break;
      }
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
    }

    public byte GetID()
    {
      return 0x03; // no ID
    }

    public void Install(ITVComputer in_parent)
    {
      m_tvcomputer = in_parent;
    }

    public void Remove(ITVComputer in_parent)
    {

    }
  }
}
