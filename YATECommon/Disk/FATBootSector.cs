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
// FAT12 Boot sector class
///////////////////////////////////////////////////////////////////////////////
using YATECommon.Helpers;

namespace YATECommon.Disk
{
  internal class FATBootSector
  {
    #region · Constants ·
    public const int BootSectorSize = 512;
    #endregion

    #region · Data members ·
    private byte[] m_boot_sector; // Data buffer
    #endregion

    #region · Constructor · 
    /// <summary>
    /// Constructs boot sector class from a sector data buffer
    /// </summary>
    /// <param name="in_buffer"></param>
    public FATBootSector(byte[] in_buffer)
    {
      m_boot_sector = in_buffer;
    }

    #endregion

    #region · Properties ·

    /// <summary>
    /// Bytes per Sector
    /// </summary>
    public ushort BytesPerSector
    {
      get { return m_boot_sector.ToUshort(11); }
      set { m_boot_sector.FromUshort(11, value); }
    }

    /// <summary>
    /// Sectors per Clusters
    /// </summary>
    public byte SectorsPerCluster
    {
      get { return m_boot_sector[13]; }
      set { m_boot_sector[13] = value; }
    }

    /// <summary>
    /// The number of sectors from the Partition Boot Sector to the start of the first file allocation table,
    /// including the Partition Boot Sector.
    /// </summary>
    public ushort ReservedSectors
    {
      get { return m_boot_sector.ToUshort(14); }
      set { m_boot_sector.FromUshort(14, value); }
    }

    /// <summary>
    /// Number of FAT tables
    /// </summary>
    public byte NumberOfFatTables
    {
      get { return m_boot_sector[16]; }
      set { m_boot_sector[16] = value; }
    }

    /// <summary>
    /// Number of Entries in the Root Directory
    /// </summary>
    public ushort NumberOfRootEntries
    {
      get { return m_boot_sector.ToUshort(17); }
      set { m_boot_sector.FromUshort(17, value); }
    }

    /// <summary>
    /// Number of Sectors on Disk. 
    /// </summary>
    public ushort NumberOfSectors
    {
      get { return m_boot_sector.ToUshort(19); }
      set { m_boot_sector.FromUshort(19, value); }
    }

    /// <summary>
    /// Type of Media
    /// </summary>
    public byte MediaType
    {
      get { return m_boot_sector[21]; }
      set { m_boot_sector[21] = value; }
    }

    /// <summary>
    /// Sectors in each FAT
    /// </summary>
    public ushort SectorsPerFat
    {
      get { return m_boot_sector.ToUshort(22); }
      set { m_boot_sector.FromUshort(22, value); }
    }

    /// <summary>
    /// Sectors in each Track
    /// </summary>
    public ushort SectorsPerTrack
    {
      get { return m_boot_sector.ToUshort(24); }
      set { m_boot_sector.FromUshort(24, value); }
    }

    /// <summary>
    /// Number of Heads (Sides)
    /// </summary>
    public ushort NumberOfHeads
    {
      get { return m_boot_sector.ToUshort(26); }
      set { m_boot_sector.FromUshort(26, value); }
    }

    /// <summary>
    /// Same as the Relative Sector field in the Partition Table.
    /// </summary>
    public ushort HiddenSectors
    {
      get { return m_boot_sector.ToUshort(28); }
      set { m_boot_sector.FromUshort(28, value); }
    }

    #endregion

    #region · Public members · 

    /// <summary>
    /// Validates boot sector based on the actual data content
    /// </summary>
    /// <returns>True if boot sector is valid</returns>
    public bool IsValid()
    {
      // check bytes per sector validity
      if (BytesPerSector != 512 && BytesPerSector != 1024 && BytesPerSector != 2048 && BytesPerSector != 4096)
        return false;
  
      // check number of sector per claster
      if (SectorsPerCluster != 1 && SectorsPerCluster != 2 && SectorsPerCluster != 4 && SectorsPerCluster != 8 && SectorsPerCluster != 16 && SectorsPerCluster != 32 && SectorsPerCluster != 64 && SectorsPerCluster != 128)
        return false;

      // check reserved sectors
      if (ReservedSectors != 1)
        return false;

      // Check number of FAT tables
      if(NumberOfFatTables != 2)
        return false;

      // Check sectors per track
      if (SectorsPerTrack != 8 && SectorsPerTrack != 9)
        return false;

      // Check number os heads
      if(NumberOfHeads != 1 && NumberOfHeads != 2) 
        return false;

      // Check hidden sectors
      if (HiddenSectors != 0)
        return false;

      int number_of_sectors = 0;
      switch(MediaType)
      {
        case 0xf8:
          if (NumberOfHeads != 1 || SectorsPerTrack != 9)
            return false;
          number_of_sectors = 80 * 9 * 1;
          break;

        case 0xf9:
          if (NumberOfHeads != 2 || SectorsPerTrack != 9)
            return false;
          number_of_sectors = 80 * 9 * 2;
          break;

        case 0xfa:
          if (NumberOfHeads != 1 || SectorsPerTrack != 8)
            return false;
          number_of_sectors = 80 * 8 * 1;
          break;

        case 0xfb:
          if (NumberOfHeads != 2 || SectorsPerTrack != 8)
            return false;
          number_of_sectors = 80 * 8 * 2;
          break;

        case 0xfc:
          if (NumberOfHeads != 1 || SectorsPerTrack != 9)
            return false;
          number_of_sectors = 40 * 9 * 1;
          break;

        case 0xfd:
          if (NumberOfHeads != 2 || SectorsPerTrack != 9)
            return false;
          number_of_sectors = 40 * 9 * 2;
          break;

        case 0xfe:
          if (NumberOfHeads != 1 || SectorsPerTrack != 8)
            return false;
          number_of_sectors = 80 * 8 * 1;
          break;

        case 0xff:
          if (NumberOfHeads != 2 || SectorsPerTrack != 8)
            return false;
          number_of_sectors = 40 * 8 * 2;
          break;

        default:
          return false;
      }

      // check total number of sectors
      if (number_of_sectors != NumberOfSectors)
        return false;

      return true;
    }

    #endregion
  }
}
