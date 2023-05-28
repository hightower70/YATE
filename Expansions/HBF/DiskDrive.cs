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
// Disk drive emulation class
///////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using YATECommon.Disk;

namespace HBF
{
	/// <summary>
	/// Virtual disk image
	/// </summary>
	class DiskDrive : IDisposable
	{
    #region · Data members ·
    private byte m_current_track = 0;
		private byte m_current_sector = 0;
		private int m_current_lba = 0;
		private IVirtualFloppyDisk m_disk_image;
		private byte[] m_sector_buffer;
		private int m_sector_byte_pos = 0;
		private FloppyDriveGeometry m_drive_geometry;
    #endregion

		public DiskDrive()
		{
			m_drive_geometry = new FloppyDriveGeometry(80, 2);
		}

    #region · Properties ·
    /// <summary>
    /// Gets current logical track (head number which is written to the disk)
    /// </summary>
    public byte LogicalTrack
		{
			get 
			{
				if (IsDoubleStepEnabled())
					return (byte)(m_current_track / 2);
				else
					return m_current_track;
      }
    }

		/// <summary>
		/// Gets/sets Physical track the track number where the head is located)
		/// </summary>
		public byte PhysicalTrack
		{
      get 
			{
				return m_current_track; 
			}
      set
      {
        if (value < 0)
          value = 0;

        if (value > m_drive_geometry.NumberOfTracks)
          value = (byte)(m_drive_geometry.NumberOfTracks - 1);

        m_current_track = value;
      }
    }

		/// <summary>
		/// Gets current sector
		/// </summary>
		public byte Sector
		{
			get { return m_current_sector; }
			set { m_current_sector = value; }
		}

		/// <summary>
		/// Gets disk geometry information
		/// </summary>
		public FloppyDiskGeometry Geometry
		{
			get 
			{
				if (m_disk_image != null)
					return m_disk_image.Geometry;

				return null;
			}
		}
    #endregion

    #region · Virtual drive builder ·
		public void BuildDiskDrive(HBFDriveSettings in_card_settings)
		{
			// clear old image
      if (m_disk_image != null)
      {
				m_disk_image.Close();
        m_disk_image.Dispose();
        m_disk_image = null;
      }
      
			// build virtual disk 
			switch (in_card_settings.EmulationMode)
			{
				// No drive
				case 0:
					break;

				// Disk image file
				case 1:
					m_disk_image = new FloppyDiskImage();
					m_disk_image.Open(in_card_settings.DiskImageFile);
          break;
			}

			// allocate sector buffer
			if(m_disk_image != null)
			{
				m_sector_buffer = new byte[m_disk_image.Geometry.SectorLength];
			}
			else
			{
				m_sector_buffer = null;
			}	 
		}

		#endregion

		/// <summary>
		/// Seeks the virtual disk image to the given position
		/// </summary>
		/// <param name="in_sector">Sector number 0..DiskGeometry.SectorPerTrack</param>
		/// <param name="in_head">Side (head) number: 0..NumberOfSides</param>
		public void SeekSector(int in_sector, int in_head)
		{
			// if disk is not inserted
			if (!IsDiskPresent())
				return;

			if (in_sector < 1 || in_sector > m_disk_image.Geometry.SectorPerTrack || in_head < 0 || in_head >= m_disk_image.Geometry.NumberOfSides)
				return;

			m_current_sector = (byte)in_sector;

			m_current_lba = (LogicalTrack * (m_disk_image.Geometry.NumberOfSides * m_disk_image.Geometry.SectorPerTrack) + in_head * m_disk_image.Geometry.SectorPerTrack + in_sector - 1);
			m_sector_byte_pos = 0;
		}

		/// <summary>
		/// Returns true if disk image is loaded
		/// </summary>
		/// <returns></returns>
		public bool IsDiskPresent()
		{
			return m_disk_image != null;
		}

		/// <summary>
		/// Returns true if double step is enabled
		/// </summary>
		/// <returns></returns>
		public bool IsDoubleStepEnabled()
		{
			if (m_disk_image == null)
				return false;

			return m_drive_geometry.NumberOfTracks >= m_disk_image.Geometry.NumberOfTracks * 2;
		}

		/// <summary>
		/// Reads one byte from the disk and advances to the next byte
		/// </summary>
		/// <returns></returns>
		public byte ReadByte()
		{
			if (m_disk_image == null)
				return 0;

			if(m_sector_byte_pos >= Geometry.SectorLength)
			{
				m_sector_byte_pos = 0;
				m_current_lba++;
			}

			// read sector
			if (m_sector_byte_pos == 0)
			{
				m_disk_image.ReadSector(m_current_lba, m_sector_buffer);
				m_sector_byte_pos = 0;
			}

			// return one byte from the sector
			return m_sector_buffer[m_sector_byte_pos++];
		}

		/// <summary>
		/// Writes one byte to the disk and advances to the next byte
		/// </summary>
		/// <param name="in_byte"></param>
		public void WriteByte(byte in_byte)
		{
			if (m_disk_image == null)
				return;

			// store byte
			m_sector_buffer[m_sector_byte_pos++] = in_byte;

			// store sector
			if (m_sector_byte_pos == Geometry.SectorLength)
			{
				m_disk_image.WriteSector(m_current_lba, m_sector_buffer);
				m_sector_byte_pos = 0;
				m_current_lba++;
			}
		}

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

			if (m_disk_image != null)
			{
				m_disk_image.Close();
				m_disk_image.Dispose();
				m_disk_image = null;
			}

			disposed = true;
		}

		~DiskDrive()
		{
			Dispose(false);
		}

		#endregion
	}
}
