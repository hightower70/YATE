﻿///////////////////////////////////////////////////////////////////////////////
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
// Main interface class for MultiCart ROM expansion card
///////////////////////////////////////////////////////////////////////////////
using SDCart.Forms;
using YATECommon;
using YATECommon.Expansions;

namespace SDCart
{
  public class ExpansionMain : ExpansionBase
  {
    public const string ModuleName = "SDCart";

    #region · Data members ·
    private ExpansionSetupPageInfo[] m_setup_page_info;
    private SDCart m_multicart;
    #endregion

    public ExpansionMain()
    {
      m_setup_page_info = new ExpansionSetupPageInfo[]
      {
        new ExpansionSetupPageInfo("Config", typeof(SetupConfiguration)),
        new ExpansionSetupPageInfo("Information", typeof(SetupInformation)),
        new ExpansionSetupPageInfo("About", typeof(SetupAbout))
      };
    }

    public override void GetExpansionInfo(ExpansionInfo inout_module_info)
    {
      base.GetExpansionInfo(inout_module_info);

      inout_module_info.Description = "SD Cart Extender";
      inout_module_info.SectionName = ModuleName;
      inout_module_info.Type = ExpansionManager.ExpansionType.Cartridge;
      inout_module_info.SetupPages = m_setup_page_info;
    }

    public override void Initialize(ExpansionManager in_expansion_manager, int in_expansion_index)
    {
      base.Initialize(in_expansion_manager, in_expansion_index);

      Settings = ParentManager.Settings.GetSettings<SDCartSettings>(in_expansion_index);
    }

    /// <summary>
    /// Installs expansion module into the computer
    /// </summary>
    /// <param name="in_computer"></param>
    public override void Install(ITVComputer in_computer)
    {
      m_multicart = new SDCart();
      m_multicart.SetSettings((SDCartSettings)Settings);

      in_computer.InsertCartridge(m_multicart);
    }

    /// <summary>
    /// Removes expansion module from the computer
    /// </summary>
    /// <param name="in_computer"></param>
    public override void Remove(ITVComputer in_computer)
    {
      in_computer.RemoveCartridge();
    }
  }
}

