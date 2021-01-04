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
// Main interface class for 32k RAM expansion card
///////////////////////////////////////////////////////////////////////////////
using SAA1099.Forms;
using YATECommon;
using YATECommon.ExpansionManager;

namespace SAA1099
{
  public class ExpansionMain : ExpansionBase
  {
    public const string ModuleName = "SAA1099";

    #region · Data members ·
    private ExpansionSetupPageInfo[] m_settings_page_info;
    #endregion

    public ExpansionMain()
    {
      m_settings_page_info = new ExpansionSetupPageInfo[]
      {
        new ExpansionSetupPageInfo("Information", typeof(SetupInformation)),
        new ExpansionSetupPageInfo("About", typeof(SetupAbout))
      };
    }

    public override void Initialize(ExpansionManager in_expansion_manager, int in_expansion_index)
    {
      base.Initialize(in_expansion_manager, in_expansion_index);

      //Settings = ParentManager.Settings.GetSettings<HBSCardSettings>();
    }


    public override void GetExpansionInfo(ExpansionInfo inout_module_info)
    {
      base.GetExpansionInfo(inout_module_info);

      inout_module_info.Description = "SAA1099 Sound Generator";
      inout_module_info.SectionName = ModuleName;
      inout_module_info.Type = ExpansionManager.ExpansionType.Hardware;
      inout_module_info.SetupPages = m_settings_page_info;
    }

    /// <summary>
    /// Installs this expansion to the emulated computer
    /// </summary>
    public override void Install(ITVComputer in_computer)
    {
    }

    /// <summary>
    /// Removes this expansion module from the emulated computer
    /// </summary>
    public override void Remove(ITVComputer in_computer)
    {
    }
  }
}
