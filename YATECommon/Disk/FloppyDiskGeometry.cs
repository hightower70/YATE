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
// Floppy disk geometry information class
///////////////////////////////////////////////////////////////////////////////

namespace YATECommon.Disk
{
  public class FloppyDiskGeometry
  {
    #region · Properties ·

    /// <summary>
    /// Number of tracks in the disk
    /// </summary>
    public int NumberOfTracks { get; set; }
    
    /// <summary>
    /// MNumber of sectors on one track
    /// </summary>
    public int SectorPerTrack { get; set; }

    /// <summary>
    /// Number of disk sides
    /// </summary>
    public int NumberOfSides { get; set; }

    /// <summary>
    /// Length of the sector in bytes
    /// </summary>
    public int SectorLength { get; set; }

    /// <summary>
    /// Gets disk size in bytes
    /// </summary>
    public int DiskSizeInBytes
    {
      get { return NumberOfTracks * SectorPerTrack * NumberOfSides * SectorLength; }
    }

    #endregion

    #region · Constructors ·

    /// <summary>
    /// Sets disk geometry parameters using the given values
    /// </summary>
    /// <param name="in_number_of_track">Number of track on the disk</param>
    /// <param name="in_sector_per_track">Number of sectors per one track</param>
    /// <param name="in_number_of_sides">Number of disk sizes</param>
    /// <param name="in_sector_length">Length of a sector in bytes</param>
    public FloppyDiskGeometry(int in_number_of_track, int in_sector_per_track, int in_number_of_sides, int in_sector_length = 512)
    {
      NumberOfTracks = in_number_of_track;
      SectorPerTrack = in_sector_per_track;
      NumberOfSides = in_number_of_sides;
      SectorLength = in_sector_length;
    }

    /// <summary>
    /// Creates disk geometry from total disk size in bytes
    /// </summary>
    /// <param name="in_total_size_in_bytes"></param>
    public FloppyDiskGeometry(int in_total_size_in_bytes)
    {
      SectorLength = 512;
      SectorPerTrack = 9;
      NumberOfSides = 2;

      int sector_count = in_total_size_in_bytes / SectorLength;

      NumberOfTracks = sector_count / NumberOfSides / SectorPerTrack;
    }

    /// <summary>
    /// Default constructor. Sets parameter for 720k, 80track, 9 sectors per track, double side disk
    /// </summary>
    public FloppyDiskGeometry()
    {
      NumberOfTracks = 80;
      SectorPerTrack = 9;
      NumberOfSides = 2;
      SectorLength = 512;
    }

    /// <summary>
    /// Creates disk geometry from media descriptor byte 
    /// </summary>
    /// <param name="in_media_descriptor"></param>
    public FloppyDiskGeometry(byte in_media_descriptor)
    {
      switch(in_media_descriptor) 
      {
        case 0xf8:
          NumberOfTracks = 80;
          SectorPerTrack = 9;
          NumberOfSides = 1;
          break;

        case 0xf9:
          NumberOfTracks = 80;
          SectorPerTrack = 9;
          NumberOfSides = 2;
          break;

        case 0xfa:
          NumberOfTracks = 80;
          SectorPerTrack = 8;
          NumberOfSides = 1;
          break;

        case 0xfb:
          NumberOfTracks = 80;
          SectorPerTrack = 8;
          NumberOfSides = 2;
          break;

        case 0xfc:
          NumberOfTracks = 40;
          SectorPerTrack = 9;
          NumberOfSides = 1;
          break;

        case 0xfd:
          NumberOfTracks = 40;
          SectorPerTrack = 9;
          NumberOfSides = 2;
          break;

        case 0xfe:
          NumberOfTracks = 40;
          SectorPerTrack = 8;
          NumberOfSides = 1;
          break;

        case 0xff:
          NumberOfTracks = 40;
          SectorPerTrack = 8;
          NumberOfSides = 2;
          break;
      }

      SectorLength = 512;
    }

    #endregion
  }
}
