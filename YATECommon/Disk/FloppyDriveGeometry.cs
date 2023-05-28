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
// Floppy drive geometry information class
///////////////////////////////////////////////////////////////////////////////

namespace YATECommon.Disk
{
  public class FloppyDriveGeometry
  {
    #region · Properties ·
    /// <summary>
    /// Number of tracks in the disk
    /// </summary>
    public int NumberOfTracks { get; set; }

    /// <summary>
    /// Number of disk sides
    /// </summary>
    public int NumberOfSides { get; set; }

    #endregion

    #region · Constructors ·

    /// <summary>
    /// Sets drive geometry parameters using the given values
    /// </summary>
    /// <param name="in_number_of_track">Number of track on the disk</param>
    /// <param name="in_number_of_sides">Number of disk sizes</param>
    public FloppyDriveGeometry(int in_number_of_track, int in_number_of_sides)
    {
      NumberOfTracks = in_number_of_track;
      NumberOfSides = in_number_of_sides;
    }

    #endregion
  }
}
