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
// Disk image file handler class
///////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;

namespace YATECommon.Disk
{
  public class FloppyDiskImage : IVirtualFloppyDisk
  {
    #region · Data members ·
    private FileStream m_disk_image;
    private FloppyDiskGeometry m_disk_geometry;
    #endregion

    #region · Constructor ·

    /// <summary>
    /// Default constructor
    /// </summary>
    public FloppyDiskImage()
    {
      m_disk_image = null;
    }
    #endregion

    #region · Properties ·

    /// <summary>
    /// Gets disk geometry
    /// </summary>
    public FloppyDiskGeometry Geometry
    {
      get { return m_disk_geometry; }
    }

    /// <summary>
    /// Returns true is disk is present
    /// </summary>
    public bool IsDiskPresent
    {
      get { return m_disk_image != null; }
    }

    #endregion

    #region · Public members ·

    /// <summary>
    /// Open disk image file
    /// </summary>
    /// <param name="in_path"></param>
    public void Open(string in_path)
    {
      try
      {
        // open or create disk image
        m_disk_image = File.Open(in_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

        // set image length
        if (m_disk_image.Length == 0)
        {
          // create an empty image
          m_disk_geometry = new FloppyDiskGeometry();

          int length = m_disk_geometry.DiskSizeInBytes;
          while (length > 0)
          {
            m_disk_image.WriteByte(0xe5);
            length--;
          }

          m_disk_image.Flush();

          m_disk_image.Seek(0, SeekOrigin.Begin);
        }
        else
        {
          // determine if disk image is FAT12
          byte[] boot_sector_buffer = new byte[FATBootSector.BootSectorSize];

          m_disk_image.Read(boot_sector_buffer, 0, boot_sector_buffer.Length);
          m_disk_image.Seek(0, SeekOrigin.Begin);

          FATBootSector boot_sector = new FATBootSector(boot_sector_buffer);

          if (boot_sector.IsValid())
          {
            // Disk image is FAT12 -> determine disk format from the Media Type byte
            m_disk_geometry = new FloppyDiskGeometry(boot_sector.MediaType);
          }
          else
          {
            // create geometry from image size
            int length = (int)(new FileInfo(in_path).Length);

            m_disk_geometry = new FloppyDiskGeometry(length);
          }
        }
      }
      catch
      {
        if(m_disk_image != null)
        {
          m_disk_image.Close();
          m_disk_image.Dispose();
          m_disk_image = null;
        }
      }
    }

    /// <summary>
    /// Closes disk image file
    /// </summary>
    public void Close() 
    {
      if (m_disk_image != null)
      {
        m_disk_image.Close();
        m_disk_image.Dispose();
        m_disk_image = null;
      }
    }
    #endregion

    #region · IVirtualFloppyDisk ·

    /// <summary>
    /// Number of all sectors omn the disk
    /// </summary>
    public int TotalSectorCount
    {
      get
      {
        if (m_disk_geometry != null)
          return m_disk_geometry.NumberOfTracks * m_disk_geometry.NumberOfSides * m_disk_geometry.SectorPerTrack;
        else
          return 0;
      }
    }

    /// <summary>
    /// Reads one sector from the disk
    /// </summary>
    /// <param name="in_linear_sector_index">Linear sector address [0...TotalSectorCount]</param>
    /// <param name="out_buffer">Buffer for receiveing sector data</param>                                            
    public void ReadSector(int in_linear_sector_index, byte[] out_buffer)
    {
      if(m_disk_image!=null)
      {
        m_disk_image.Seek(in_linear_sector_index * m_disk_geometry.SectorLength, SeekOrigin.Begin);
        m_disk_image.Read(out_buffer, 0, out_buffer.Length);
      }
    }

    /// <summary>
    /// Writes one sector
    /// </summary>
    /// <param name="in_linear_sector_index">Linear sector address [0...TotalSectorCount]</param>
    /// <param name="in_buffer">Data buffer containing bytes to write into the sector</param>
    public void WriteSector(int in_linear_sector_index, byte[] in_buffer)
    {
      if (m_disk_image != null)
      {
        m_disk_image.Seek(in_linear_sector_index * m_disk_geometry.SectorLength, SeekOrigin.Begin);
        m_disk_image.Write(in_buffer, 0, in_buffer.Length);
      }
    }

    #endregion

    #region · Disposable interface ·

    bool disposed = false;

    // Public implementation of Dispose pattern callable by consumers.
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
      if (disposed)
        return;

      if (disposing)
      {
        Close();
      }

      disposed = true;
    }

    #endregion
  }
}
