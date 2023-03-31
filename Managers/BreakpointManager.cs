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
// Manages TVC breakpoints
///////////////////////////////////////////////////////////////////////////////
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon;

namespace YATE.Managers
{
  public class BreakpointManager : IBreakpointManager, INotifyPropertyChanged
  {
    #region · Data members ·
    private HashSet<ushort> m_breakpoint_addresses;
    #endregion

    #region · Properties ·
    public ObservableCollection<BreakpointInfo> Breakpoints { get; private set; }
    public bool IsBreakpointsExists { get; private set; }


    // Delete all breakpoints command
    public ManagerCommand DeleteAllBreakpointsCommand { get; private set; }

    #endregion

    /// <summary>
    /// Default constructor
    /// </summary>
    public BreakpointManager()
    {
      Breakpoints = new ObservableCollection<BreakpointInfo>();
      m_breakpoint_addresses = new HashSet<ushort>();

      DeleteAllBreakpointsCommand = new ManagerCommand(DeleteAllBreakpoints);
    }

    /// <summary>
    /// Deletes all breakpoints
    /// </summary>
    /// <param name="parameter"></param>
    public void DeleteAllBreakpoints(object parameter)
    {
      Breakpoints.Clear();
      m_breakpoint_addresses.Clear();
    }

    /// <summary>
    /// Check if breakpoint exists at the given address
    /// </summary>
    /// <param name="in_address"></param>
    /// <returns></returns>
    public bool IsBreakpointExistsAtAddress(ushort in_address)
    {
      return m_breakpoint_addresses.Contains(in_address);
    }

    /// <summary>
    /// Gets breakpoint index based on the properties
    /// </summary>
    /// <param name="in_memory_type">Type of the memory where the breakpoint is located</param>
    /// <param name="in_address">Address of the breakpoint</param>
    /// <param name="in_page_index">Memory Page index where the breakpoint in located</param>
    /// <returns></returns>
    public int GetBreakpointIndex(TVCMemoryType in_memory_type, ushort in_address, ushort in_page_index)
    {
      for (int i = 0; i < Breakpoints.Count; i++)
      {
        if (Breakpoints[i].IsEqual(in_memory_type, in_address, in_page_index))
        {
          return i;
        }
      }

      return -1;
    }

    /// <summary>
    /// Adds a new breakpoint to the collection
    /// </summary>
    /// <param name="in_breakpoint"></param>
    public void AddBreakpoint(BreakpointInfo in_breakpoint)
    {
      // check if breakpoint already exists
      if (Breakpoints.IndexOf(in_breakpoint) < 0)
      {
        Breakpoints.Add(in_breakpoint);
        UpdateBreakpointAddressList();
      }
    }

    /// <summary>
    /// Removes breakpoint from the list
    /// </summary>
    /// <param name="in_breakpoint"></param>
    public void RemoveBreakpoint(BreakpointInfo in_breakpoint)
    {
      // check if breakpoint exists
      int breakpoint_index = Breakpoints.IndexOf(in_breakpoint);
      if (breakpoint_index >= 0)
      {
        Breakpoints.RemoveAt(breakpoint_index);
        UpdateBreakpointAddressList();
      }
    }


    /// <summary>
    /// Updates breakpoint address hash list
    /// </summary>
    private void UpdateBreakpointAddressList()
    {
      // update breakpoint existance flag
      IsBreakpointsExists = Breakpoints.Count > 0;

      // update address list
      m_breakpoint_addresses.Clear();

      for (int i = 0; i < Breakpoints.Count; i++)
      {
        m_breakpoint_addresses.Add(Breakpoints[i].Address);
      }
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
