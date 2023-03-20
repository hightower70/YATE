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
// Main interface class for MultiCart ROM expansion card
///////////////////////////////////////////////////////////////////////////////
using NanoSD.Forms;
using YATECommon;
using YATECommon.Expansions;

namespace NanoSD
{
  public class ExpansionMain : ExpansionBase
  {
    public const string ModuleName = "NanoSD";

    #region · Data members ·
    private ExpansionSetupPageInfo[] m_setup_page_info;
    private NanoSDCard m_nano_sd_card;
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

      inout_module_info.Description = "Nano SD Card";
      inout_module_info.SectionName = ModuleName;
      inout_module_info.Type = ExpansionManager.ExpansionType.Card;
      inout_module_info.SetupPages = m_setup_page_info;
    }

    public override void Initialize(ExpansionManager in_expansion_manager, int in_expansion_index)
    {
      base.Initialize(in_expansion_manager, in_expansion_index);

      Settings = ParentManager.Settings.GetSettings<NanoSDCardSettings>(in_expansion_index);
    }

    /// <summary>
    /// Installs expansion module into the computer
    /// </summary>
    /// <param name="in_computer"></param>
    public override void Install(ITVComputer in_computer)
    {
      m_nano_sd_card = new NanoSDCard(this);
      m_nano_sd_card.SetSettings((NanoSDCardSettings)Settings);
      in_computer.InsertCard(((NanoSDCardSettings)Settings).SlotIndex, m_nano_sd_card);
    }

    /// <summary>
    /// Removes expansion module from the computer
    /// </summary>
    /// <param name="in_computer"></param>
    public override void Remove(ITVComputer in_computer)
    {
      in_computer.RemoveCard(((NanoSDCardSettings)Settings).SlotIndex);
    }

    /// <summary>
    /// Called when settings has been changed
    /// </summary>
    public override void SettingsChanged(ref bool in_restart_tvc)
    {
      // update settings
      Settings = ParentManager.Settings.GetSettings<NanoSDCardSettings>(ExpansionIndex);

      // activate settings
      if (m_nano_sd_card.SetSettings((NanoSDCardSettings)Settings))
        in_restart_tvc = true;
    }
  }
}

