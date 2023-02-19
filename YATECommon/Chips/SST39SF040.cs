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
// SST39SF040 Flash ROM Chip Emulation
///////////////////////////////////////////////////////////////////////////////
using YATECommon.Helpers;

namespace YATECommon.Chips
{
  public class SST39SF040
  {
    #region · Constants ·
    public const int FlashSize = 512 * 1024;
    #endregion

    #region · Types ·

    /// <summary>
    /// FLash operation mode
    /// </summary>
    private enum OperationMode
    {
      Normal,
      Identification,
      ChipErase,
      ByteWrite
    };
    #endregion

    #region · Data members ·
    private byte[] m_rom;
    
    // flash rom emulation registers
    private OperationMode m_flash_mode;
    private int m_flash_command_sequence;
    #endregion

    #region · Properties ·
    public string ROMFileName { get; set; }
    public bool ROMContentChanged { get; set; }
    #endregion

    #region · Public members ·

    /// <summary>
    /// Default constructor
    /// </summary>
    public SST39SF040()
    {
      m_rom = new byte[FlashSize];
      Clear();
      ROMContentChanged = false;
      m_flash_mode = OperationMode.Normal;
      m_flash_command_sequence = 0;
    }

    /// <summary>
    /// Clears flash memory content (sets all bytes to 0xff)
    /// </summary>
    public void Clear()
    {
      ROMContentChanged = true;

      ROMFile.FillMemory(m_rom);
    }

    /// <summary>
    /// Sets file name for storing ROM content
    /// </summary>
    /// <param name="in_filename">Binary file name to store flash content</param>
    /// <returns>True if memory content has been changed</returns>
    public bool Load()
    {
      bool rom_content_changed = false;

      // reload rom content
      byte[] new_rom_content = new byte[FlashSize];
      ROMFile.LoadMemoryFromFile(ROMFileName, new_rom_content);

      // compare new memory content
      if (!ROMFile.IsMemoryEqual(m_rom, new_rom_content))
      {
        m_rom = new_rom_content;
        rom_content_changed = true;
      }

      ROMContentChanged = false;

      return rom_content_changed;
    }

    /// <summary>
    /// Saves flash rom content a the binary file
    /// </summary>
    /// <returns>True if save was success</returns>
    public bool Save()
    {
      ROMContentChanged = false;
      return ROMFile.SaveMemoryToFile(ROMFileName, m_rom);
    }

    /// <summary>
    /// Writes memory content
    /// </summary>
    /// <param name="in_address">Address to write</param>
    /// <param name="in_byte">Data byte to write</param>
    public void MemoryWrite(int in_address, byte in_byte)
    {
      if (in_address >= FlashSize || in_address < 0)
        return;

      if (m_flash_mode == OperationMode.ByteWrite)
      {
        m_rom[in_address] = in_byte;
        ROMContentChanged = true;
        ResetFlashMode();
      }
      else
      {
        // state machine for write handler
        switch (m_flash_command_sequence)
        {
          // command sequence 1st byte
          case 0:
            if (in_address == 0x5555)
            {
              switch (in_byte)
              {
                // command leading byte
                case 0xaa:
                  m_flash_command_sequence++;
                  break;

                // exit command, reset 
                case 0xf0:
                  ResetFlashMode();
                  break;
              }
            }
            else
            {
              ResetFlashMode();
            }
            break;

          // command sequence 2nd byte
          case 1:
            if (in_address == 0x2aaa)
            {
              if (in_byte == 0x55)
                m_flash_command_sequence++;
              else
                ResetFlashMode();
            }
            else
            {
              ResetFlashMode();
            }
            break;

          // Command sequence 3rd byte
          case 2:
            if (in_address == 0x5555)
            {
              switch (in_byte)
              {
                // ID mode
                case 0x90:
                  m_flash_command_sequence = 0;
                  m_flash_mode = OperationMode.Identification;
                  break;

                // exit from ID mode
                case 0xf0:
                  m_flash_command_sequence = 0;
                  m_flash_mode = OperationMode.Normal;
                  break;

                //chip erase
                case 0x80:
                  m_flash_mode = OperationMode.ChipErase;
                  m_flash_command_sequence++;
                  break;

                // write data
                case 0xa0:
                  m_flash_mode = OperationMode.ByteWrite;
                  m_flash_command_sequence++;
                  break;

                default:
                  m_flash_command_sequence = 0;
                  break;
              }
            }
            else
            {
              ResetFlashMode();
            }
            break;

          // command sequence 4th byte
          case 3:
            if (in_address == 0x5555)
            {
              switch (m_flash_mode)
              {
                case OperationMode.ChipErase:
                  if (in_byte == 0xaa)
                    m_flash_command_sequence++;
                  else
                    ResetFlashMode();
                  break;

                default:
                  ResetFlashMode();
                  break;
              }
            }
            else
            {
              ResetFlashMode();
            }
            break;

          // command sequence 5th byte
          case 4:
            if (in_address == 0x2aaa)
            {
              switch (m_flash_mode)
              {
                case OperationMode.ChipErase:
                  if (in_byte == 0x55)
                    m_flash_command_sequence++;
                  else
                    ResetFlashMode();
                  break;

                default:
                  ResetFlashMode();
                  break;
              }
            }
            else
            {
              ResetFlashMode();
            }
            break;

          // command sequence 6th byte
          case 5:
            if (in_address == 0x5555)
            {
              switch (m_flash_mode)
              {
                case OperationMode.ChipErase:
                  // chip erase
                  if (in_byte == 0x10)
                  {
                    // execute chip erase
                    Clear();
                  }
                  ResetFlashMode();
                  break;

                default:
                  ResetFlashMode();
                  break;
              }
            }
            else
            {
              ResetFlashMode();
            }
            break;

          default:
            ResetFlashMode();
            break;
        }
      }
    }

    /// <summary>
    /// Reads from flash memory 
    /// </summary>
    /// <param name="in_address"></param>
    /// <returns></returns>
    public byte MemoryRead(int in_address)
    {
      switch (m_flash_mode)
      {
        case OperationMode.Normal:
        case OperationMode.ByteWrite:
          if (in_address >= 0 && in_address < FlashSize)
            return m_rom[in_address];
          else
            return 0xff;

        case OperationMode.Identification:
          switch (in_address)
          {
            case 0:
              return 0xbf;

            case 1:
              return 0xb7;

            default:
              return 0xff;
          }


        default:
          return 0xff;
      }
    }

    #endregion

    #region · Private members ·

    /// <summary>
    /// Resets flash operation mode
    /// </summary>
    private void ResetFlashMode()
    {
      m_flash_mode = OperationMode.Normal;
      m_flash_command_sequence = 0;
    }

    #endregion
  }
}
