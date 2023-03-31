///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2020 Laszlo Arvai. All rights reserved.
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
// Stores oen breakpoint data
///////////////////////////////////////////////////////////////////////////////
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon;
using YATECommon.Helpers;

namespace YATE.Managers
{
  public class BreakpointInfo : INotifyPropertyChanged
  {
    /// <summary>
    /// Memory type where the breakpoint is located
    /// </summary>
    public TVCMemoryType MemoryType { get; set; }

    /// <summary>
    /// Memory Address were the breakpoint is located
    /// </summary>
    public ushort Address { get; set; }

    /// <summary>
    /// Page index where the breakpoint is located (if more than one page exists in the memory, otherwise 0)
    /// </summary>
    public ushort Page { get; set; }

    /// <summary>
    /// Creates breakpoint info
    /// </summary>
    /// <param name="in_memory_type">Memory type</param>
    /// <param name="in_address">Memory address</param>
    /// <param name="in_page">page index</param>
    public BreakpointInfo(TVCMemoryType in_memory_type, ushort in_address, ushort in_page = 0)
    {
      MemoryType = in_memory_type;
      Address = in_address;
      Page = in_page;
    }

    /// <summary>
    /// Compares two breakpoint classes, returns true when their content is same
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
      BreakpointInfo breakpoint_info = obj as BreakpointInfo;
      if (breakpoint_info != null)
      {
        return (MemoryType == breakpoint_info.MemoryType && Address == breakpoint_info.Address && Page == breakpoint_info.Page);
      }

      return false;
    }

    /// <summary>
    /// Compares breakpoint classes with the specifies properties
    /// </summary>
    /// <param name="in_memory_type"></param>
    /// <param name="in_address"></param>
    /// <param name="in_page"></param>
    /// <returns></returns>
    public bool IsEqual(TVCMemoryType in_memory_type, ushort in_address, ushort in_page = 0)
    {
      return in_memory_type == MemoryType && in_address == Address && in_page == Page;
    }

    /// <summary>
    /// Converts breakpoint info to readable string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return string.Format("0x{0:X4} ({1},0x{2:X4})", Address, MemoryType.ToString(),Page);
    }

    public override int GetHashCode()
    {
      return HashCodeHelper.Hash<ushort, TVCMemoryType, ushort>(Address, MemoryType, Page);
    }

    #region · INotifyPropertyHandler ·

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      if (PropertyChanged != null && propertyName != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
