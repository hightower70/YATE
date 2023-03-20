///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2013-2014 Laszlo Arvai. All rights reserved.
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
// Module settings base class
///////////////////////////////////////////////////////////////////////////////
using System.Runtime.Serialization;

namespace YATECommon.Settings
{
  public class SettingsBase
  {
    public enum SettingsCategory
    {
      Emulator,
      EmulatorIndexed,
      TVC
    };

    [IgnoreDataMember]
    public SettingsCategory Category { get; set; }

    [IgnoreDataMember]
    public string ModuleName { get; set; }

    public SettingsBase(SettingsCategory in_category, string in_module_name)
    {
      Category = in_category;
      ModuleName = in_module_name;
    }

    public virtual void SetDefaultValues()
    {
    }

    public override bool Equals(object obj)
    {
      if(obj is SettingsBase)
        return ModuleName.Equals(((SettingsBase)obj).ModuleName);

      return false;
    }

    public override int GetHashCode()
    {
      return ModuleName.GetHashCode();
    }
  }
}
