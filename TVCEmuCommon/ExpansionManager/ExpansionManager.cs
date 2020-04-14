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
using TVCEmuCommon.Settings;

namespace TVCEmuCommon.ExpansionManager
{
  /// <summary>
  /// Manages expansions (enumerates, loads, etc.)
  /// </summary>
  public class ExpansionManager
	{
		#region · Data members ·

		private static ExpansionManager m_default = null;
		private Type m_main_module_type = null;
		private List<ExpansionBase> m_modules = new List<ExpansionBase>();
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
      main_module.Initialize(this);

      LoadExpansions();
    }


    #endregion

    #region · Singleton members ·

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static ExpansionManager Default
		{
			get
			{
				if (m_default == null)
				{
					m_default = new ExpansionManager(SettingsFile.Default);
				}

				return m_default;
			}
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
		public List<ExpansionBase> Modules
		{
			get { return m_modules; }
		}

    /// <summary>
    /// Main module
    /// </summary>
    public Type MainModuleType
    {
      get { return m_main_module_type; }
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
			List<ModuleBaseSettingsInfo> module_list = m_settings_file.GetModuleList();

			m_modules.Clear();

      // handle main module
      if (m_main_module_type != null)
      {
        ExpansionBase main_module = (ExpansionBase)Activator.CreateInstance(m_main_module_type);
        main_module.Initialize(this);

        m_modules.Add(main_module);
      }

      // load modules
			for (int i = 0; i < module_list.Count; i++)
			{
				if (module_list[i].Active)
				{
					ExpansionBase module;
          ExpansionInfo expansion_info;

					LoadExpansionMainClass(module_list[i].ModuleName, out module, out expansion_info);

					m_modules.Add(module);
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
      for (int i = 0; i < m_modules.Count; i++)
      {
        m_modules[i].Install(in_computer);
      }
    }

    /// <summary>
    /// Removes all expansion modules
    /// </summary>
    public void RemoveExpansions()
		{
			for (int i = 0; i < m_modules.Count; i++)
			{
				m_modules[i].Remove();
			}
		}

		
		#endregion

		#region · Setup (options settings) helper functions ·

		/// <summary>
		/// Refreshes module info collection
		/// </summary>
		public void SetupRefreshModuleInfo()
		{
			string[] module_files;
      string path = GetModulePath();
			ExpansionBase main_class;
      ExpansionInfo expansion_info;

			module_files = Directory.GetFiles(path, "*.extension.dll");

			m_available_modules.Clear();
			for (int i = 0; i < module_files.Length; i++)
			{
				if (LoadExpansionMainClass(Path.GetFileNameWithoutExtension(module_files[i]), out main_class, out expansion_info))
				{
          main_class.GetExpansionInfo(expansion_info);

					m_available_modules.Add(expansion_info);
				}
			}
		}

		/// <summary>
		/// Add module in setup operation
		/// </summary>
		/// <param name="in_module_name"></param>
		public void SetupAddModule(string in_module_name)
		{
			// load module
			ExpansionBase module;
      ExpansionInfo expansion_info;

			LoadExpansionMainClass(in_module_name, out module, out expansion_info);

			m_modules.Add(module);

			// create tree information
			SetupCreateModuleTreeInfo(module, m_modules.Count - 1);
		}

		public void SetupRemoveModule(int in_module_index)
		{
			// do not remove main module
			if (in_module_index == 0)
				return;

			// stops module
			m_modules[in_module_index].Remove();

			// remove module
			m_modules.RemoveAt(in_module_index);
			m_module_setup_tree_info.RemoveAt(in_module_index);
		}

		/// <summary>
		/// Generates ModuleSetupTreeInfo collection
		/// </summary>
		public void SetupBuildTreeInfo()
		{
			int module_index;
			m_module_setup_tree_info.Clear();

			for (module_index = 0; module_index < m_modules.Count; module_index++)
			{
				SetupCreateModuleTreeInfo(m_modules[module_index], module_index);
			}
		}

		/// <summary>
		/// Creates tree info of one module
		/// </summary>
		/// <param name="in_expansion"></param>
		/// <param name="in_index"></param>
		private void SetupCreateModuleTreeInfo(ExpansionBase in_expansion, int in_index)
		{
      ExpansionInfo expansion_info = new ExpansionInfo();
      in_expansion.GetExpansionInfo(expansion_info);

			if (expansion_info.SetupPages.Length > 0)
			{
				// create tree item
				ExpansionSetupTreeInfo info = new ExpansionSetupTreeInfo(expansion_info.Description, expansion_info.SetupPages[0], in_index, -1);
				info.IsExpanded = true;

				for (int page_index = 0; page_index < expansion_info.SetupPages.Length; page_index++)
				{
					info.AddChild(new ExpansionSetupTreeInfo(expansion_info.SetupPages[page_index], in_index, page_index));
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
    private bool LoadExpansionMainClass(string in_filename, out ExpansionBase out_interface_class, out ExpansionInfo out_expansion_info)
		{
			string filename;
			Assembly module_assembly;

			// generate filename
			filename = GetModulePath();
			filename = Path.Combine(filename, in_filename);
			filename += ".dll";

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
              out_interface_class.Initialize(this);

              out_expansion_info = new ExpansionInfo(in_filename, module_assembly.GetName().Version.ToString());

              return true;
						}
					}
				}
			}
			catch
			{
				out_interface_class = null;
        out_expansion_info = null;
				return false;
			}

			out_interface_class = null;
      out_expansion_info = null;
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

#if false

		/// <summary>
		/// Adds a new module to the list of active modules
		/// </summary>
		/// <param name="in_module"></param>
		public void AddModule(ModuleInfo in_module)
		{
			List<SettingsFileBase.ModuleInfo> module_list = FrameworkSettingsFile.Default.GetModuleList();
			SettingsFileBase.ModuleInfo search_entry = new SettingsFileBase.ModuleInfo();
			string name;
			int i;
			ModuleBase module_class;

			// check if this module already exists but not active
			name = in_module.DLLName;

			i = 0;
			while (i < module_list.Count)
			{
				if (module_list[i].ModuleName == name && !module_list[i].Active)
					break;

				i++;
			}

			// check if there was an inactive configuration data
			if (i < module_list.Count)
			{
				// activate inactive module
				//FrameworkSettingsFile.Default.ModuleActivate(in_module);
			}
			else
			{
				// create a new module
				SettingsFileBase.ModuleInfo module_info = new SettingsFileBase.ModuleInfo(in_module.SectionName, in_module.DLLName, true);

				FrameworkSettingsFile.Default.ModuleAdd(module_info);
			}

			LoadModule(in_module.DLLName, out module_class);

		
			// create module class
			m_modules.Add(module_class);

			// add module settings info to the tree
			int module_index = m_modules.Count - 1;

			ModuleSettingsInfo[] settings_info = m_modules[module_index].GetSettingsInfo();
			ModuleSettingsTreeInfo info = new ModuleSettingsTreeInfo(m_modules[module_index].GetDisplayName(), settings_info[0], module_index, -1);
			info.IsExpanded = true;

			for (int page_index = 0; page_index < settings_info.Length; page_index++)
			{
				info.AddChild(new ModuleSettingsTreeInfo(settings_info[page_index], module_index, page_index));
			}
			m_module_setup_tree_info.Add(info);
		}


            
#region · Deep copy ·

		/// <summary>
		/// Copy modules information from another instance of this class
		/// </summary>
		/// <param name="in_module_manager"></param>
		public void CopyModulesFrom(ExpansionManager in_module_manager, SettingsFile in_settings)
		{
			// copy main module
			m_main_module = in_module_manager.m_main_module;

			// copy module list
			m_modules.Clear();

			for (int i = 0; i < in_module_manager.m_modules.Count; i++)
			{
				ExpansionBase module = (ExpansionBase)Activator.CreateInstance(in_module_manager.m_modules[i].GetType());

				module.CopyFrom(in_module_manager.m_modules[i]);
				module.ModuleManager = this;
				module.ModuleSettings = in_settings;

				m_modules.Add(module);
			}

			// clear other members
			m_module_setup_tree_info.Clear();
			m_available_modules.Clear();
		}

		/// <summary>
		/// Searches modules for the display panel with a given name
		/// </summary>
		/// <param name="in_name">Name of the display pane to search for</param>
		/// <returns>Panel if found, null if not found</returns>
		public FrameworkElement GetModuleDisplayPanel(string in_name)
		{
			FrameworkElement retval = null;
							 /*
			for (int i = 0; i < m_modules.Count && retval == null; i++)
			{
				retval = m_modules[i].GetDisplayPanel(in_name);
			}
								*/
			return retval;
		}

#endregion

#endif