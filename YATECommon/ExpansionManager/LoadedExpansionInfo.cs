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
// Loaded Expansion module information class
///////////////////////////////////////////////////////////////////////////////

using System.IO;

namespace YATECommon.ExpansionManager
{
  /// <summary>
  /// Module information
  /// </summary>
  public class LoadedExpansionInfo
  {
    #region · Properties ·

    public ExpansionBase ExpansionClass { get; set; } = null;

    public string SectionName
    {
      get
      {
        ExpansionInfo info = new ExpansionInfo();
        ExpansionClass.GetExpansionInfo(info);

        return info.SectionName;
      }
    }

    public string Description
    {
      get
      {
        ExpansionInfo info = new ExpansionInfo();
        ExpansionClass.GetExpansionInfo(info);

        return info.Description;
      }
    }

    public string VersionString
    {
      get
      {
        ExpansionInfo info = new ExpansionInfo();
        ExpansionClass.GetExpansionInfo(info);

        return info.VersionString;
      }
    }

    public ExpansionManager.ExpansionType Type
    {
      get
      {
        ExpansionInfo info = new ExpansionInfo();
        ExpansionClass.GetExpansionInfo(info);

        return info.Type;
      }
    }

    public int ModuleIndex { get; set; } = 0;
    public int SlotIndex { get; set; } = -1;

    #endregion

    #region · Constructor ·

    public LoadedExpansionInfo(ExpansionBase in_expansion_class)
    {
      ExpansionClass = in_expansion_class;
    }

    public LoadedExpansionInfo(ExpansionBase in_expansion_class, int in_module_index)
    {
      ExpansionClass = in_expansion_class;
      ModuleIndex = in_module_index;
      SlotIndex = -1;
    }

    public LoadedExpansionInfo(ExpansionBase in_expansion_class, int in_module_index, int in_slot_index)
    {
      ExpansionClass = in_expansion_class;
      ModuleIndex = in_module_index;
      SlotIndex = in_slot_index;
    }

    #endregion

    #region · Member functions ·

    public override string ToString()
    {
      return Description;
    }

    #endregion
  }
}
