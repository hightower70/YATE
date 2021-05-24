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
// Tree item for setup dialog
///////////////////////////////////////////////////////////////////////////////

using CustomControls.TreeViewItemBase;

namespace YATECommon.Expansions
{
  /// <summary>
  /// Setup dialog tree item
  /// </summary>
	public class ExpansionSetupTreeInfo : TreeViewItemBase
	{
    /// <summary>
    /// Construct tree item
    /// </summary>
    /// <param name="in_settings">Setup page info</param>
    /// <param name="in_module_index">Module index (if more than one module is used of the same expansion type)</param>
    /// <param name="in_form_index"></param>
		public ExpansionSetupTreeInfo(ExpansionSetupPageInfo in_settings, int in_module_index, int in_form_index, int in_slot_index)
		{
			DisplayName = in_settings.PageName;
			Settings = in_settings;
			ModuleIndex = in_module_index;
			FormIndex = in_form_index;
      SlotIndex = in_slot_index;
		}

		public ExpansionSetupTreeInfo(string in_display_name, ExpansionSetupPageInfo in_settings, int in_module_index, int in_form_index, int in_slot_index)
		{
			DisplayName = in_display_name;
			Settings = in_settings;
			ModuleIndex = in_module_index;
			FormIndex = in_form_index;
      SlotIndex = in_slot_index;
    }

    public string DisplayName { get; private set; }
		public ExpansionSetupPageInfo Settings { get; set; }
		public int ModuleIndex { get; private set; }
		public int FormIndex { get; private set; }
    public int SlotIndex { get; private set; }
  }
}
