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
// Fopply disk geometry information class
///////////////////////////////////////////////////////////////////////////////
using System;

namespace YATECommon.Disk
{
  public interface IVirtualFloppyDisk : IDisposable
  {
    int TotalSectorCount { get; }

    FloppyDiskGeometry Geometry { get; }

    bool IsDiskPresent { get; }

    void ReadSector(int in_linear_sector_index, byte[] out_buffer);
    void WriteSector(int in_linear_sector_index, byte[] in_buffer);

    void Open(string in_path);
    void Close();
  }
}