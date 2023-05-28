using System;
using System.IO;

namespace YATECommon.Disk
{
  public class FloppyDiskImage : IVirtualFloppyDisk
  {
    private FileStream m_disk_image;
    private FloppyDiskGeometry m_disk_geometry;


    public FloppyDiskImage()
    {
      m_disk_image = null;
    }

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
          // create geometry
          int length = (int)(new FileInfo(in_path).Length);

          m_disk_geometry = new FloppyDiskGeometry(length);
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


    #region · IVirtualFloppyDisk ·

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

    public void ReadSector(int in_linear_sector_index, byte[] out_buffer)
    {
      if(m_disk_image!=null)
      {
        m_disk_image.Seek(in_linear_sector_index * m_disk_geometry.SectorLength, SeekOrigin.Begin);
        m_disk_image.Read(out_buffer, 0, out_buffer.Length);
      }
    }

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
