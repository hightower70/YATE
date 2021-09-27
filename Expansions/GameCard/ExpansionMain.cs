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
using GameCard.Forms;
using YATECommon;
using YATECommon.Expansions;

namespace GameCard
{
  public class ExpansionMain : ExpansionBase
  {
    public const string ModuleName = "GameCard";

    #region · Data members ·
    private ExpansionSetupPageInfo[] m_setup_page_info;
    private GameCard m_game_card;
    #endregion

    public ExpansionMain()
    {
      m_setup_page_info = new ExpansionSetupPageInfo[]
      {
        new ExpansionSetupPageInfo("Config", typeof(SetupConfiguration)),
        new ExpansionSetupPageInfo("Joystick", typeof(SetupJoystick)),
        new ExpansionSetupPageInfo("Information", typeof(SetupInformation)),
        new ExpansionSetupPageInfo("About", typeof(SetupAbout))
      };
    }

    public override void GetExpansionInfo(ExpansionInfo inout_module_info)
    {
      base.GetExpansionInfo(inout_module_info);

      inout_module_info.Description = "Game Card";
      inout_module_info.SectionName = ModuleName;
      inout_module_info.Type = ExpansionManager.ExpansionType.Card;
      inout_module_info.SetupPages = m_setup_page_info;
    }

    public override void Initialize(ExpansionManager in_expansion_manager, int in_expansion_index)
    {
      base.Initialize(in_expansion_manager, in_expansion_index);

      Settings = ParentManager.Settings.GetSettings<GameCardSettings>(in_expansion_index);
    }

    /// <summary>
    /// Installs expansion module into the computer
    /// </summary>
    /// <param name="in_computer"></param>
    public override void Install(ITVComputer in_computer)
    {
      m_game_card = new GameCard(this);
      m_game_card.SetSettings((GameCardSettings)Settings);
      in_computer.InsertCard(((GameCardSettings)Settings).SlotIndex, m_game_card);
    }

    /// <summary>
    /// Removes expansion module from the computer
    /// </summary>
    /// <param name="in_computer"></param>
    public override void Remove(ITVComputer in_computer)
    {
      in_computer.RemoveCard(((GameCardSettings)Settings).SlotIndex);
    }

    public override void SettingsChanged(ref bool in_restart_tvc)
    {
      // updatre settingsd
      Settings = ParentManager.Settings.GetSettings<GameCardSettings>(ExpansionIndex);

      // activate settings
      if (m_game_card.SetSettings((GameCardSettings)Settings))
        in_restart_tvc = true;
    }
  }
}

