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
// Main module interface
///////////////////////////////////////////////////////////////////////////////
using YATE.Dialogs;
using YATECommon.ExpansionManager;

namespace YATE.Settings
{
  public class MainModule : ExpansionBase
  {
    ExpansionSetupPageInfo[] m_setup_page_info;

    public MainModule()
    {
      //ModuleName = GetDisplayName();
      m_setup_page_info = new ExpansionSetupPageInfo[]
      {
        new ExpansionSetupPageInfo("TVComputer", typeof(SetupTVCConfiguration)),
        new ExpansionSetupPageInfo("Input", typeof(SetupInput)),
        new ExpansionSetupPageInfo("Audio", typeof(SetupAudio)),
        new ExpansionSetupPageInfo("Gamebase", typeof(SetupGamebase)),
        new ExpansionSetupPageInfo("General", typeof(SetupGeneral))
      };
    }

    public override void GetExpansionInfo(ExpansionInfo inout_module_info)
    {
      inout_module_info.Description = "Emulator";
      inout_module_info.SectionName = "Main";
      inout_module_info.Type = ExpansionManager.ExpansionType.Unknown;
      inout_module_info.SetupPages = m_setup_page_info;
    }
  }
}
