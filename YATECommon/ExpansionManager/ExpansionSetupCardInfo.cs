﻿///////////////////////////////////////////////////////////////////////////////
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
// Expansion card information class
///////////////////////////////////////////////////////////////////////////////

namespace YATECommon.ExpansionManager
{
  public class ExpansionSetupCardInfo
  {
    public string Description { get; }
    public string ModuleName { get; }
    public int SlotIndex { get; }

    public ExpansionSetupCardInfo(string in_description, string in_module_name, int in_slot_index)
    {
      Description = in_description;
      ModuleName = in_module_name;
      SlotIndex = in_slot_index;
    }
  }
}
