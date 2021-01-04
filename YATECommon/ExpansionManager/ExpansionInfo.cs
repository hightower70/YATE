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
// Expansion module information class
///////////////////////////////////////////////////////////////////////////////

using System.IO;
using YATECommon.Settings;

namespace YATECommon.ExpansionManager
{
	/// <summary>
	/// Module information
	/// </summary>
	public class ExpansionInfo
	{
    #region · Properties ·

    public string ModuleName { get; set; }     
		public string SectionName { get; set; }
		public string Description { get; set; }
		public string VersionString { get; set; }
		public string ModuleIndex { get; set; }

    public ExpansionManager.ExpansionType Type { get; set; }

    public ExpansionSetupPageInfo[] SetupPages { get; set; }

    #endregion
    #region · Constructor ·
    public ExpansionInfo()
    {
    }

    public ExpansionInfo(string in_module_name)
    {
      ModuleName = in_module_name;
    }

    #endregion

    #region · Member functions ·

    public ExpansionSettingsBase ConvertToSettings(int in_slot_index)
    {
      ExpansionSettingsBase expansion_settings;

      if (Type == ExpansionManager.ExpansionType.Card)
        expansion_settings = new CardSettingsBase(SettingsBase.SettingsCategory.TVC, SectionName, in_slot_index);
      else
        expansion_settings = new ExpansionSettingsBase(SettingsBase.SettingsCategory.TVC, SectionName);

      return expansion_settings;
    }

    public override string ToString()
		{
			return Description;
		}

		public override bool Equals(object in_object)
		{
			// If this and obj do not refer to the same type, then they are not equal.
			if (in_object.GetType() != this.GetType())
				return false;

			return (ModuleName == ((ExpansionInfo)in_object).ModuleName) && (ModuleIndex == ((ExpansionInfo)in_object).ModuleIndex);
    }

		public override int GetHashCode()
		{
			return new { ModuleName, ModuleIndex }.GetHashCode();
		}

		#endregion
	}
}
