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
// Expansion settings base class
///////////////////////////////////////////////////////////////////////////////
using System.Xml.Serialization;
using YATECommon.Helpers;

namespace YATECommon.Settings
{
  public class ExpansionSettingsBase : SettingsBase
  {
    [XmlAttribute]
    public int ExpansionIndex { get; set; }

    [XmlAttribute]
    public bool Active { get; set; }

    public ExpansionSettingsBase(SettingsCategory in_category, string in_expansion_name) : base( in_category, in_expansion_name)
    {
      ExpansionIndex = -1;
      Active = false;
    }

    public ExpansionSettingsBase(ExpansionSettingsBase in_module_settings_base) : base(in_module_settings_base.Category, in_module_settings_base.ModuleName)
    {
      ExpansionIndex = in_module_settings_base.ExpansionIndex;
      Active = in_module_settings_base.Active;
    }

    public override void SetDefaultValues()
    {
      base.SetDefaultValues();

      Active = false;
      ExpansionIndex = -1;
    }

    public override bool Equals(object obj)
    {
      if(obj is ExpansionSettingsBase)
      {
        return base.Equals(obj) && ExpansionIndex == ((ExpansionSettingsBase)obj).ExpansionIndex;
      }

      return false;
    }

    public override int GetHashCode()
    {
      return HashCodeHelper.Hash(base.GetHashCode(), ExpansionIndex.GetHashCode());
    }

  }
}
