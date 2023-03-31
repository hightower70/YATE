///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2021 Laszlo Arvai. All rights reserved.
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
// Card settings base class
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Xml.Serialization;
using YATECommon.Helpers;

namespace YATECommon.Settings
{
  public class CardSettingsBase : ExpansionSettingsBase
  {
    [XmlAttribute]
    public int SlotIndex { get; set; }

    public CardSettingsBase(SettingsCategory in_category, string in_module_name) : base(in_category, in_module_name)
    {
      Active = false;
      ExpansionIndex = -1;
      SlotIndex = -1;
    }

    public CardSettingsBase(CardSettingsBase in_settings_base) : base(in_settings_base.Category, in_settings_base.ModuleName)
    {
      Active = in_settings_base.Active;
      ExpansionIndex = in_settings_base.ExpansionIndex;
      SlotIndex = in_settings_base.SlotIndex;
    }

    public CardSettingsBase(SettingsCategory in_category, string in_module_name, int in_slot_index) : base(in_category, in_module_name)
    {
      Active = false;
      ExpansionIndex = -1;
      SlotIndex = in_slot_index;
    }

    public override void SetDefaultValues()
    {
      base.SetDefaultValues();

      Active = false;
      ExpansionIndex = -1;
      SlotIndex = -1;
    }

    public override bool Equals(object obj)
    {
      if(obj is CardSettingsBase)
      {
        return base.Equals(obj) && SlotIndex == ((CardSettingsBase)obj).SlotIndex;
      }

      return false;
    }

    public override int GetHashCode()
    {
      return HashCodeHelper.Hash(base.GetHashCode(), SlotIndex.GetHashCode());
    }

    public TVCMemoryType GetCardMemoryType()
    {
      switch (SlotIndex)
      {
        case 0:
          return TVCMemoryType.Slot0;

        case 1:
          return TVCMemoryType.Slot1;

        case 2:
          return TVCMemoryType.Slot2;

        case 3:
          return TVCMemoryType.Slot2;

        default:
          throw new ArgumentOutOfRangeException("Invalid card index");
      }
    }
  }
}
