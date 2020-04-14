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
// Main interface class for EPROM programmer expansion card
///////////////////////////////////////////////////////////////////////////////

using System.Reflection;
using HBE.Forms;
using TVCEmuCommon;
using TVCEmuCommon.ExpansionManager;

namespace HBE
{
  /// <summary>
  /// EPROM programmer card main class
  /// </summary>
  public class ExpansionMain : ExpansionBase
  {
    #region · Data members ·
    private readonly ExpansionSetupPageInfo[] m_settings_page_info;
    private HBECard m_eprom_programmer_card;
    #endregion

    public ExpansionMain()
    {
      // Setp pages
      m_settings_page_info = new ExpansionSetupPageInfo[]
      {
        new ExpansionSetupPageInfo("Config", typeof(SetupConfiguration)),
        new ExpansionSetupPageInfo("Information", typeof(SetupInformation)),
        new ExpansionSetupPageInfo("About", typeof(SetupAbout))
      };
    }

    /// <summary>
    /// Gets expansion card info
    /// </summary>
    /// <param name="inout_module_info"></param>
    public override void GetExpansionInfo(ExpansionInfo inout_module_info)
    {
      base.GetExpansionInfo(inout_module_info);

      inout_module_info.Description = "EPROM Programmer Card";
      inout_module_info.SectionName = "HBE";
      inout_module_info.Type = ExpansionInfo.ExpansionType.Card;
      inout_module_info.SetupPages = m_settings_page_info;
    }

    /// <summary>
    /// Initializes this expansion module
    /// </summary>
    /// <param name="in_expansion_manager">Manager who is the owner of this module</param>
    /// <param name="in_expansion_assembly">Expansion module DLL assembly</param>
    public override void Initialize(ExpansionManager in_expansion_manager)
    {
      base.Initialize(in_expansion_manager);

      Settings = ParentManager.Settings.GetSettings<HBECardSettings>();
    }

    /// <summary>
    /// Initializes expansion module
    /// </summary>
    /// <param name="in_computer"></param>
    public override void Install(ITVComputer in_computer)
    {
      m_eprom_programmer_card = new HBECard();
      m_eprom_programmer_card.SetSettings((HBECardSettings)Settings);
      in_computer.InsertCard(((HBECardSettings)Settings).SlotIndex, m_eprom_programmer_card);
    }
  }
}
