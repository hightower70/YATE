using System;
using System.IO;

namespace TVCHardware
{
	/// <summary>
	/// Virtual disk image
	/// </summary>
	class DiskDrive : IDisposable
	{
		public class DiskGeometry
		{
			public int NumberOfTracks = 80;
			public int SectorPerTrack = 9;
			public int NumberOfSides = 2;
			public int SectorLength = 512;
		}

		private FileStream m_disk_image = null;
		private byte m_current_track = 0;
		private int m_current_sector = 0;
		private DiskGeometry m_disk_geometry = new DiskGeometry();

		/// <summary>
		/// Gets current track
		/// </summary>
		public byte Track
		{
			get { return m_current_track; }
			set { m_current_track = value; }
		}

		/// <summary>
		/// Gets disk geometry information
		/// </summary>
		public DiskGeometry Geometry
		{
			get { return m_disk_geometry; }
		}

		/// <summary>
		/// Seeks the virtual disk image to the given position
		/// </summary>
		/// <param name="in_track">Track number 0..DiskGeometry.NumberOfTracks</param>
		/// <param name="in_sector">Sector number 0..DiskGeometry.SectorPerTrack</param>
		/// <param name="in_head">Side (head) number: 0..NumberOfSides</param>
		public void SeekSector(int in_sector, int in_head)
		{
			if (m_disk_image == null)
				return;

			if (Track < 0 || Track >= m_disk_geometry.NumberOfTracks || in_sector < 1 || in_sector > m_disk_geometry.SectorPerTrack || in_head < 0 || in_head >= m_disk_geometry.NumberOfSides)
				return;

			m_current_track = Track;
			m_current_sector = in_sector;

			int lba = (m_current_track * (m_disk_geometry.NumberOfSides * m_disk_geometry.SectorPerTrack) + in_head * m_disk_geometry.SectorPerTrack + in_sector - 1) * Geometry.SectorLength;
			m_disk_image.Seek(lba, SeekOrigin.Begin);
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
		/// Opens disk image file
		/// </summary>
		/// <param name="in_image_file_name"></param>
		public void OpenDiskImageFile(string in_image_file_name)
		{
			// open or create disk image
			m_disk_image = File.Open(in_image_file_name, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

			// set image length
			if (m_disk_image.Length == 0)
			{
				int length = GetDiskSizeInBytes();
				while (length > 0)
				{
					m_disk_image.WriteByte(0);
					length--;
				}

				m_disk_image.Flush();

				m_disk_image.Seek(0, SeekOrigin.Begin);
			}
		}


		/// <summary>
		/// Closes disk image file
		/// </summary>
		public void CloseDiskImageFile()
		{
			if(m_disk_image !=null)
			{
				m_disk_image.Close();
				m_disk_image = null;
			}
		}

		/// <summary>
		/// Gets disk size in bytes
		/// </summary>
		/// <returns>Disk size in bytes</returns>
		public int GetDiskSizeInBytes()
		{
			return m_disk_geometry.NumberOfTracks * m_disk_geometry.SectorPerTrack * m_disk_geometry.NumberOfSides * m_disk_geometry.SectorLength;
		}

		/// <summary>
		/// Reads one byte from the disk and advances to the next byte
		/// </summary>
		/// <returns></returns>
		public byte ReadByte()
		{
			if (m_disk_image == null)
				return 0;

			return (byte)m_disk_image.ReadByte();
		}

		/// <summary>
		/// Writes one byte to the disk and advances to the next byte
		/// </summary>
		/// <param name="in_byte"></param>
		public void WriteByte(byte in_byte)
		{
			if (m_disk_image == null)
				return;

			m_disk_image.WriteByte(in_byte);
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

			if (disposing)
			{
				CloseDiskImageFile();
			}

			disposed = true;
		}

		#endregion
	}
}
