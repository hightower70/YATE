///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2013-2014 Laszlo Arvai. All rights reserved.
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
// EPROM programmer (HBE) card emulation
///////////////////////////////////////////////////////////////////////////////

using YATECommon;
using YATECommon.Chips;

namespace HBE
{
  /// <summary>
  /// EPROM programmer (HBE) card emulation class
  /// </summary>
  public class HBECard : ITVCCard
  {
    #region · Type ·
    private const int MaxEEPROMSize = 32768;

    public enum EPROMType
    {
      Unknown,
      E2K,
      E4K,
      E8K,
      E16K,
      E32K
    }

    private enum EPROMMode
    {
      Unknown,
      Read,
      Write
    }
    #endregion

    private ITVComputer m_tvcomputer;

    private I8255 m_ppi1;
    private I8255 m_ppi2;

    //private EPROMType m_eprom_type;
    //private EPROMMode m_eprom_mode;
    private ushort m_eprom_address;
    private byte m_eprom_data;
    private byte[] m_eprom_buffer;

    private HBECardSettings m_settings;

    public void SetSettings(HBECardSettings in_settings)
    {
      m_settings = in_settings;
    }

    #region · ITVCCard interface ·

    /// <summary>
    /// Gets card hardware ID (not used)
    /// </summary>
    /// <returns></returns>
    public byte GetID()
    {
      return 0x03;
    }

    /// <summary>
    /// Initializes card class
    /// </summary>
    /// <param name="in_parent"></param>
    public void Insert(ITVComputer in_parent)
    {
      m_tvcomputer = in_parent;

      // init internal variables
      m_eprom_buffer = new byte[MaxEEPROMSize];
      //m_eprom_type = EPROMType.Unknown;
      //m_eprom_mode = EPROMMode.Unknown;

      // Create PPI chips
      m_ppi1 = new I8255();
      m_ppi2 = new I8255();

      // set event handlers
      m_ppi1.PortAChanged += PPI1PortAChanged;
      m_ppi1.PortBChanged += PPI1PortBChanged;
      m_ppi1.PortCChanged += PPI1PortCChanged;

      //m_eprom_mode = EPROMMode.Unknown;
    }

    /// <summary>
    /// Removes card from the system
    /// </summary>
    public void Remove(ITVComputer in_parent)
    {
      // no action needed
    }

    /// <summary>
    ///  Reads memory content (no memory, not implemented)
    /// </summary>
    /// <param name="in_address"></param>
    /// <returns></returns>
    public byte MemoryRead(ushort in_address)
    {
      return 0xff;
    }

    /// <summary>
    /// Writes card's memory content (non memory, not implemented
    /// </summary>
    /// <param name="in_address"></param>
    /// <param name="in_byte"></param>
    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // no card memory
    }

    /// <summary>
    /// Preiodic 
    /// </summary>
    /// <param name="in_cpu_tick"></param>
    public void PeriodicCallback(ulong in_cpu_tick)
    {
      // not required
    }

    /// <summary>
    /// Reads card's registers
    /// </summary>
    /// <param name="in_address"></param>
    /// <param name="inout_data"></param>
    public void PortRead(ushort in_address, ref byte inout_data)
    {
      if ((in_address & 0x04) == 0)
      {
        m_ppi1.PortRead((ushort)(in_address & 0x03), ref inout_data);
      }
      else
      {
        m_ppi2.PortRead((ushort)(in_address & 0x03), ref inout_data);
      }
    }

    /// <summary>
    /// Writes card's register
    /// </summary>
    /// <param name="in_address"></param>
    /// <param name="in_byte"></param>
    public void PortWrite(ushort in_address, byte in_byte)
    {
      if((in_address & 0x04)==0)
      {
        m_ppi1.PortWrite((ushort)(in_address & 0x03), in_byte);
      }
      else
      {
        m_ppi2.PortWrite((ushort)(in_address & 0x03), in_byte);
      }
    }

    public void Reset()
    {
      m_ppi1.Reset();
      m_ppi2.Reset();
    }
    #endregion

    private void PPI1PortAChanged(object sender, I8255.PortChangedEventArgs args)
    {
      if (m_ppi1.IsPortAOutput)
        m_eprom_data = args.NewValue;
    }

    private void PPI1PortBChanged(object sender, I8255.PortChangedEventArgs args)
    {
      if (m_ppi1.IsPortBOutput)
        m_eprom_address = (ushort)(m_eprom_address & 0xff00 | args.NewValue);
    }

    private void PPI1PortCChanged(object sender, I8255.PortChangedEventArgs args)
    {
      if (m_ppi1.IsPortCLoOutput)
        m_eprom_address = (ushort)(m_eprom_address & 0xf0ff | ((args.NewValue << 8) & 0x0f));

      if (m_ppi1.IsPortCHiOutput)
        m_eprom_address = (ushort)(m_eprom_address & 0x0fff | ((args.NewValue << 8) & 0xf0));

    }

    private void EPROMPoweredUp()
    {
      
    }
  }
}
