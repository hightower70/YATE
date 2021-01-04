using MultiCart;
using System;
using System.IO;
using YATECommon;

namespace Multicart
{
  public class TVCMultiCart : ITVCCartridge
  {
    private MultiCartSettings m_settings;

    public const int MaxCartRomSize = 1024 * 1024;
    public const int MaxCartRamSize = 512 * 1024;
    public const int RamAddressMask = MaxCartRamSize - 1;

    public byte[] Rom { get; private set; }
    public byte[] Ram { get; private set; }

    private byte m_register = 0;
    private int m_page_start_address = 0;
    private int m_register_start_address = 0;
    private bool m_ram_select = false;
    private bool m_register_locked = false;

    private int m_chip_id_sequence;
    private bool m_chip_id_mode;

    public void SetSettings(MultiCartSettings in_settings)
    {
      m_settings = in_settings;

      // load ROM
      Rom = new byte[MaxCartRomSize];
      for (int i = 0; i < Rom.Length; i++)
        Rom[i] = 0xff;

      LoadROMContent(m_settings.ROM1FileName, 0);
      LoadROMContent(m_settings.ROM2FileName, 512 * 1024);

      // init RAM
      Ram = new byte[MaxCartRamSize];

      // initialize members
      m_register = 0;
      m_page_start_address = 0;
      m_register_start_address = 0;
      m_ram_select = false;
      m_register_locked = false;
      m_chip_id_sequence = 0;
      m_chip_id_mode = false;
    }

    private void LoadROMContent(string in_rom_file_name, int in_address)
    {
      if (!string.IsNullOrEmpty(in_rom_file_name))
      {
        byte[] data = File.ReadAllBytes(in_rom_file_name);

        int length = data.Length;

        if ((in_address + data.Length) > Rom.Length)
          length = Rom.Length - in_address;

        Array.Copy(data, 0, Rom, in_address, length);
      }
    }

    public byte MemoryRead(ushort in_address)
    {
      if (m_chip_id_mode)
      {
        switch (in_address)
        {
          case 0:
            return 0xbf;

          case 1:
            return 0xb7;

          default:
            return 0xff;
        }
      }
      else
      {
        if (!m_register_locked && in_address >= m_register_start_address && in_address < m_register_start_address + 4)
          SetRegister(0);

        if (m_ram_select)
        {
          return Ram[(m_page_start_address + in_address) & RamAddressMask];
        }
        else
          return Rom[m_page_start_address + in_address];
      }
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      if (in_address >= m_register_start_address && in_address < m_register_start_address + 4)
      {
        SetRegister(in_byte);
      }
      else
      {
        if (m_ram_select)
        {
          Ram[(m_page_start_address + in_address) & RamAddressMask] = in_byte;
        }
        else
        {
          int chip_address = (m_page_start_address + in_address) & 0x7ffff;

          switch (chip_address)
          {
            case 0x5555:
              switch (m_chip_id_sequence)
              {
                case 0:
                  if (in_byte == 0xaa)
                    m_chip_id_sequence++;
                  break;

                case 2:
                  switch (in_byte)
                  {
                    case 0x90:
                      m_chip_id_sequence = 0;
                      m_chip_id_mode = true;
                      break;

                    case 0xf0:
                      // exit from ID mode
                      m_chip_id_sequence = 0;
                      m_chip_id_mode = false;
                      break;

                    default:
                      m_chip_id_sequence = 0;
                      break;
                  }
                  break;

                default:
                  m_chip_id_sequence = 0;
                  break;
              }
              break;

            case 0x2aaa:
              if (m_chip_id_sequence == 1 && in_byte == 0x55)
                m_chip_id_sequence++;
              break;

            default:
              if (in_byte == 0xf0)
              {
                // exit ID mode
                m_chip_id_mode = false;
              }
              m_chip_id_sequence = 0;
              break;

          }
        }
      }

      m_register_locked = in_address >= m_register_start_address + 2 && in_address < m_register_start_address + 4;
    }

    private void SetRegister(byte in_byte)
    {
      m_register = in_byte;

      m_ram_select = (m_register & (1 << 6)) != 0;
      m_register_start_address = ((m_register & (1 << 7)) != 0) ? 0x2000 : 0x0000;
      m_page_start_address = (m_register & 0x3f) * 0x4000;
    }

    public void Reset()
    {
      if (!m_register_locked)
        SetRegister(0);
    }

    public void Initialize(ITVComputer in_parent)
    {

    }

    public void Remove(ITVComputer in_parent)
    {

    }
  }
}
