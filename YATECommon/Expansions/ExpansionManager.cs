///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2013 Laszlo Arvai. All rights reserved.
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
// Expansion manager (Load, start, stop module, enumerate modules, etc.)
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using YATECommon.Settings;

namespace YATECommon.Expansions
{
  /// <summary>
  /// Manages expansions (enumerates, loads, etc.)
  /// </summary>
  public class ExpansionManager
	{
    #region · Constants ·
    private const string ExpansionFileExtension = ".expansion.dll";
    #endregion 

    #region · Types ·

    /// <summary>
    /// Expansion types
    /// </summary>
    public enum ExpansionType
    {
      Unknown, 

      Card,
      Cartridge,
      Hardware
    }

    #endregion

    #region · Data members ·

		private Type m_main_module_type = null;
		private List<LoadedExpansionInfo> m_expansions = new List<LoadedExpansionInfo>();
		private ObservableCollection<ExpansionSetupTreeInfo> m_module_setup_tree_info = new ObservableCollection<ExpansionSetupTreeInfo>();
    private List<ExpansionInfo> m_available_modules = new List<ExpansionInfo>();
    private SettingsFile m_settings_file;

		#endregion

		#region · Constructor ·

    /// <summary>
    /// Default constructor 
    /// </summary>
    /// <param name="in_settings_file"></param>
		public ExpansionManager(SettingsFile in_settings_file)
		{
      m_settings_file = in_settings_file;
		}

    /// <summary>
    /// Copy modules information from another instance of this class
    /// </summary>
    /// <param name="in_module_manager"></param>
    public ExpansionManager(ExpansionManager in_module_manager, SettingsFile in_settings_file)
    {
      m_settings_file = in_settings_file;

      // copy main module
      m_main_module_type = in_module_manager.m_main_module_type;

      ExpansionBase main_module = (ExpansionBase)Activator.CreateInstance(m_main_module_type);
      main_module.Initialize(this, -1);

      LoadExpansions();
    }


    #endregion

		#region · Properties ·

    /// <summary>
    /// Gets settings file for the module manager
    /// </summary>
    public SettingsFile Settings
    {
      get { return m_settings_file; }
    }

    /// <summary>
    /// List of currently loaded modules
    /// </summary>
		public List<LoadedExpansionInfo> Expansions
		{
			get { return m_expansions; }
		}

		/// <summary>
		/// Module information collection. (RefreshModuleInfo must be called before using this property)
		/// </summary>
		public List<ExpansionInfo> AvailableModules
		{
			get { return m_available_modules; }
		}

		/// <summary>
		/// Gets ModuleSetupTreeInfo collection for displaying module setup information in a tree
		/// </summary>
		public ObservableCollection<ExpansionSetupTreeInfo> ModuleSetupTreeInfo
		{
			get { return m_module_setup_tree_info; }
		}

    /// <summary>
    /// Gets ModuleSetupTreeInfo collection for displaying module setup information in a tree
    /// </summary>
    public List<ExpansionSetupCardInfo> CardSetupInfo { get; } = new List<ExpansionSetupCardInfo>();

    #endregion

    #region · Modules handler functions ·

    /// <summary>
    /// Adds main module
    /// </summary>
    /// <param name="in_main_module"></param>
    public void AddMainModule(Type in_main_module_type)
		{
			m_main_module_type = in_main_module_type;
		}

		/// <summary>
		/// Loads all expansion modules specified in the settings file
		/// </summary>
		/// <returns></returns>
		public bool LoadExpansions()
		{
			m_expansions.Clear();

      // handle main module
      if (m_main_module_type != null)
      {
        ExpansionBase main_module = (ExpansionBase)Activator.CreateInstance(m_main_module_type);
        main_module.Initialize(this, -1);

        m_expansions.Add(new LoadedExpansionInfo(main_module));
      }

      // load modules
      List<ExpansionSettingsBase> module_list = m_settings_file.GetExpansionList();
      for (int i = 0; i < module_list.Count; i++)
			{
				if (module_list[i].Active)
				{
					ExpansionBase expansion;

					LoadExpansionMainClass(module_list[i].ModuleName, out expansion);
          expansion.Initialize(this, module_list[i].ExpansionIndex);

          if (module_list[i] is CardSettingsBase)
          {
            CardSettingsBase card_info = module_list[i] as CardSettingsBase;

            m_expansions.Add(new LoadedExpansionInfo(expansion, module_list[i].ExpansionIndex, card_info.SlotIndex));
          }
          else
          {
            m_expansions.Add(new LoadedExpansionInfo(expansion, module_list[i].ExpansionIndex));
          }
				}
			}

			return true;
		}

    /// <summary>
		/// Installs expansion modules and starts them
		/// </summary>
		public void InstallExpansions(ITVComputer in_computer)
    {
      // initialize modules
      for (int i = 0; i < m_expansions.Count; i++)
      {
        m_expansions[i].ExpansionClass.Install(in_computer);
      }
    }

    /// <summary>
    /// Removes all expansion modules
    /// </summary>
    public void RemoveExpansions(ITVComputer in_computer)
		{
			for (int i = 0; i < m_expansions.Count; i++)
			{
				m_expansions[i].ExpansionClass.Remove(in_computer);
			}
		}

    /// <summary>
    /// Updates settigns
    /// </summary>
    public void SettingsChanged(ITVComputer in_computer, ref bool in_restart_tvc)
    {
      bool reload_modules = false;

      List<ExpansionSettingsBase> module_list = m_settings_file.GetExpansionList();
      List<ExpansionSettingsBase> old_module_list = SettingsFile.Previous.GetExpansionList();

      // check if module config has been changed
      if (module_list.Count != old_module_list.Count)
      {
        reload_modules = true;
      }
      else
      {
        for (int i = 0; i < module_list.Count && !reload_modules; i++)
        {
          if (!module_list[i].Equals(old_module_list[i]))
            reload_modules = true;
        }
      }

      if (reload_modules)
      {
        // remove current expansions
        for (int i = 0; i < m_expansions.Count; i++)
        {
          m_expansions[i].ExpansionClass.Remove(in_computer);
        }

        // load new expansions
        LoadExpansions();
        InstallExpansions(in_computer);
      }
      else
      {
        // update settings for all expansions
        for (int i = 0; i < m_expansions.Count; i++)
        {
          m_expansions[i].ExpansionClass.SettingsChanged(ref in_restart_tvc);
        }
      }

      if (reload_modules)
        in_restart_tvc = true;
    }

    #endregion

    #region · Setup (options settings) helper functions ·

    /// <summary>
    /// Add module in setup operation
    /// </summary>
    /// <param name="in_module_name"></param>
    public void SetupAddModule(ExpansionInfo in_expansion_info, int in_selected_slot_index)
		{
      ExpansionSettingsBase expansion_settings = in_expansion_info.ConvertToSettings(in_selected_slot_index);
      expansion_settings.Active = true;
      m_settings_file.ModuleAdd(expansion_settings);

      ExpansionBase expansion_class;

      LoadExpansionMainClass(in_expansion_info.SectionName, out expansion_class);

      LoadedExpansionInfo loaded_expansion_info = new LoadedExpansionInfo(expansion_class, expansion_settings.ExpansionIndex, in_selected_slot_index);

      m_expansions.Add(loaded_expansion_info);

			// create tree information
		  SetupCreateModuleTreeInfo(loaded_expansion_info, m_expansions.Count - 1);
		}

		public void SetupRemoveModule(int in_module_index)
		{
			// do not remove main module
			if (m_expansions[in_module_index].Type == ExpansionType.Unknown)                                                                                     
				return;

			// remove module
			m_expansions.RemoveAt(in_module_index);
			m_module_setup_tree_info.RemoveAt(in_module_index);
		}

    /// <summary>
    /// Refreshes cards information
    /// </summary>
    public void SetupRefreshCardInfo()
    {
      ExpansionBase main_class;

      List<CardSettingsBase> card_settings = m_settings_file.GetCardList();
      CardSetupInfo.Clear();
      for (int i = 0; i < TVComputerConstants.ExpansionCardCount; i++)
        CardSetupInfo.Add(new ExpansionSetupCardInfo(string.Empty, string.Empty, i));

      // add cards
      for (int i = 0; i < card_settings.Count; i++)
      {
        if (card_settings[i].SlotIndex >= 0 && card_settings[i].SlotIndex < TVComputerConstants.ExpansionCardCount && card_settings[i].Active)
        {
          if (LoadExpansionMainClass(card_settings[i].ModuleName, out main_class))
          {
            ExpansionInfo expansion_info = new ExpansionInfo();

            main_class.GetExpansionInfo(expansion_info);

            CardSetupInfo[card_settings[i].SlotIndex] = new ExpansionSetupCardInfo(expansion_info.Description, card_settings[i].ModuleName, card_settings[i].SlotIndex);
          }
        }
      }
    }
     
    /// <summary>
    /// Refreshes available module info collection
    /// </summary>
    public void SetupRefreshAvailableExpansionInfo()
    {
      string[] module_files;
      string path = GetModulePath();
      ExpansionBase main_class;

      module_files = Directory.GetFiles(path, "*" + ExpansionFileExtension);

      m_available_modules.Clear();
      for (int i = 0; i < module_files.Length; i++)
      {
        if (LoadExpansionMainClass(module_files[i], out main_class))
        {
          ExpansionInfo expansion_info = new ExpansionInfo();

          main_class.GetExpansionInfo(expansion_info);

          m_available_modules.Add(expansion_info);
        }
      }
    }

    /// <summary>
    /// Generates ModuleSetupTreeInfo collection
    /// </summary>
    public void SetupBuildTreeInfo()
		{
			int expansion_index;
			m_module_setup_tree_info.Clear();

			for (expansion_index = 0; expansion_index < m_expansions.Count; expansion_index++)
			{
				SetupCreateModuleTreeInfo(m_expansions[expansion_index], expansion_index);
			}
		}

		/// <summary>
		/// Creates tree info of one module
		/// </summary>
		/// <param name="in_expansion"></param>
		/// <param name="in_index"></param>
		private void SetupCreateModuleTreeInfo(LoadedExpansionInfo in_expansion, int in_index)
		{
      ExpansionInfo expansion_info = new ExpansionInfo();
      in_expansion.ExpansionClass.GetExpansionInfo(expansion_info);

			if (expansion_info.SetupPages.Length > 0)
			{
				// create tree item
				ExpansionSetupTreeInfo info = new ExpansionSetupTreeInfo(expansion_info.Description, expansion_info.SetupPages[0], in_index, -1, in_expansion.SlotIndex);
				info.IsExpanded = true;

				for (int page_index = 0; page_index < expansion_info.SetupPages.Length; page_index++)
				{
					info.AddChild(new ExpansionSetupTreeInfo(expansion_info.SetupPages[page_index], in_index, page_index, in_expansion.SlotIndex));
				}

				m_module_setup_tree_info.Add(info);
			}
		}

    #endregion

    #region · Non-public members ·

    /// <summary>
    /// Loads one module file
    /// </summary>
    /// <param name="in_filename"></param>
    /// <param name="out_interface_class"></param>
    /// <returns></returns>
    private bool LoadExpansionMainClass(string in_filename, out ExpansionBase out_interface_class)
		{
			string filename;
			Assembly module_assembly;

			// generate filename
			filename = GetModulePath();
			filename = Path.Combine(filename, in_filename);
      if (!filename.EndsWith(ExpansionFileExtension, StringComparison.CurrentCultureIgnoreCase))
        filename += ExpansionFileExtension;

			// load dll
			try
			{
				// load the assembly
				module_assembly = Assembly.LoadFrom(filename);

				// Walk through each type in the assembly looking for our class
				foreach (Type type in module_assembly.GetTypes())
				{
					if (type.IsClass == true)
					{
						if (type.FullName.EndsWith(".ExpansionMain"))
						{
							// create an instance of the object
							out_interface_class = (ExpansionBase)Activator.CreateInstance(type);

              return true;
						}
					}
				}
			}
			catch
			{
				out_interface_class = null;
				return false;
			}

			out_interface_class = null;
			return false;
		}

		/// <summary>
		/// Gets the path of the current executable
		/// </summary>
		/// <returns></returns>
		private string GetModulePath()
		{
			String path = Assembly.GetExecutingAssembly().Location;

			return Path.GetDirectoryName(path);
		}

		#endregion
	}
}
