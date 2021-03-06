﻿///////////////////////////////////////////////////////////////////////////////
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
// Expansion card settings base class
///////////////////////////////////////////////////////////////////////////////

using System.Xml.Serialization;

namespace TVCEmuCommon.Settings
{
  public class CardBaseSettings : BaseSettings
  {
    [XmlAttribute]
    public int SlotIndex { get; set; }

    [XmlAttribute]
    public int ModuleIndex { get; set; }

    public CardBaseSettings(string in_module_name, string in_section_name) : base(in_module_name, in_section_name)
    {
      SlotIndex = 0;
      ModuleIndex = 0;
    }

    public override void SetDefaultValues()
    {
      base.SetDefaultValues();

      ModuleIndex = 0;
      SlotIndex = 0;
    }
  }
}
