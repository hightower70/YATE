///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2018-2021 Laszlo Arvai. All rights reserved.
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
// Base class for expansion main class
///////////////////////////////////////////////////////////////////////////////

using System.Reflection;
using YATECommon.Settings;

namespace YATECommon.ExpansionManager
{
  /// <summary>
  /// Expansion (hardware modules, cards) base class
  /// </summary>
	public class ExpansionBase
  {
    #region · Properties ·

    public SettingsBase Settings { get; set; }

    public ExpansionManager ParentManager { get; private set; }

    public int ExpansionIndex { get; private set; }

    #endregion

    #region · Virtual functions to override ·

    /// <summary>
    /// Initializes this class. Settings must be loaded in the overloaded functions.
    /// </summary>
    /// <param name="in_expansion_manager"></param>
    /// <param name="in_expansion_assembly"></param>
    public virtual void Initialize(ExpansionManager in_expansion_manager, int in_expansion_index)
    {
      ParentManager = in_expansion_manager;
      ExpansionIndex = in_expansion_index;
    }

    /// <summary>
    /// Gets information about this expansion
    /// </summary>
    /// <param name="inout_module_info"></param>
    public virtual void GetExpansionInfo(ExpansionInfo inout_module_info)
    {
      inout_module_info.VersionString = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }

    /// <summary>
    /// Installs this expansion to the emulated computer
    /// </summary>
    public virtual void Install(ITVComputer in_computer)
    {
    }

    /// <summary>
    /// Removes this expansion module from the emulated computer
    /// </summary>
    public virtual void Remove(ITVComputer in_computer)
    {
    }


    #endregion
  }
}

