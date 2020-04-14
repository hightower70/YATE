﻿///////////////////////////////////////////////////////////////////////////////
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
// System Setup Dialog
///////////////////////////////////////////////////////////////////////////////
using TVCEmuCommon.Settings;
using System.Windows;
using TVCEmuCommon.ExpansionManager;
using TVCEmu.Settings;
using TVCEmuCommon.SetupPage;

namespace TVCEmu.Dialogs
{
	/// <summary>
	/// Interaction logic for SetupDialog.xaml
	/// </summary>
	public partial class SetupDialog : Window
	{
		private ExpansionManager m_current_expansion_manager;

		public SetupDialog()
		{
      // copy current settings to a temporary storage
      SettingsFile.Editing.CopySettingsFrom(SettingsFile.Default);

			// copy current module information
			m_current_expansion_manager = new ExpansionManager(ExpansionManager.Default, SettingsFile.Editing);

			InitializeComponent();
		}

		private void bAddModule_Click(object sender, RoutedEventArgs e)
		{
			m_current_expansion_manager.SetupRefreshModuleInfo();

			AddModuleDialog dialog = new AddModuleDialog();
			dialog.Owner = this;
			dialog.lbModules.DataContext = m_current_expansion_manager;

			dialog.ShowDialog();

			if(dialog.DialogResult ?? false)
			{
				foreach (ExpansionInfo module in dialog.lbModules.SelectedItems)
				{
					// load module
					m_current_expansion_manager.SetupAddModule(module.DLLName);

					// add module list
					ModuleBaseSettingsInfo module_info = new ModuleBaseSettingsInfo(module.SectionName, module.DLLName, true);
          SettingsFile.Editing.ModuleAdd(module_info);
				}
			}
		}

		private void bOK_Click(object sender, RoutedEventArgs e)
		{
			ChangeSetupPage(null); 
			this.DialogResult = true;
		}

		private void tvSetupTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{			
			int module_index;
			int page_index;

			if (e.NewValue is ExpansionSetupTreeInfo)
			{
				FrameworkElement new_page;

				// get module and page index
				module_index = (e.NewValue as ExpansionSetupTreeInfo).ModuleIndex;
				page_index = (e.NewValue as ExpansionSetupTreeInfo).FormIndex;

				// the module main node will display the same settings form as the first form
				if (page_index < 0)
					page_index = 0;

        // get new page
        ExpansionInfo expansion_info = new ExpansionInfo();
        m_current_expansion_manager.Modules[module_index].GetExpansionInfo(expansion_info);
        new_page = expansion_info.SetupPages[page_index].Form;

				ChangeSetupPage(new_page);
			}			
		}

		private void ChangeSetupPage(FrameworkElement in_new_page)
		{
			FrameworkElement old_page;
				
			// get old page
			if (gSetupFormContainer.Children.Count > 0)
				old_page = (FrameworkElement)gSetupFormContainer.Children[0];
			else
				old_page = null;

			// do nothing if the same page is selected
			if (old_page != null && in_new_page != null && old_page.GetType() == in_new_page.GetType())
				return;

			gSetupFormContainer.Children.Clear();

			// event arg
			SetupPageBase.SetupPageEventArgs event_args = new SetupPageBase.SetupPageEventArgs();
			event_args.NewPage = in_new_page;
			event_args.OldPage = old_page;

			// call changed event handler of the old page
			if (old_page is SetupPageBase)
				((SetupPageBase)old_page).OnSetupPageDeactivating(this, event_args);

			// add new page to the container
			if (in_new_page != null)
			{
				gSetupFormContainer.Children.Add(in_new_page);

				if (in_new_page is SetupPageBase)
					((SetupPageBase)in_new_page).OnSetupPageActivating(this, event_args);
			}		
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{		
			gSetupFormContainer.Children.Clear();

			SetupDialogSettings settings = SettingsFile.Editing.GetSettings<SetupDialogSettings>();

			settings.DialogPos.SaveWindowPositionAndSize(this);

      SettingsFile.Editing.SetSettings(settings);
		}

		private void Window_Initialized(object sender, System.EventArgs e)
		{			 
			SetupDialogSettings settings = SettingsFile.Editing.GetSettings<SetupDialogSettings>();

			settings.DialogPos.LoadWindowPositionAndSize(this);

			m_current_expansion_manager.SetupBuildTreeInfo();

			tvSetupTree.DataContext = m_current_expansion_manager;
		}

		private void bRemoveModule_Click(object sender, RoutedEventArgs e)
		{
			// find parent node
			ExpansionSetupTreeInfo module_tree_info = tvSetupTree.SelectedItem as ExpansionSetupTreeInfo;

			// sanity check
			if (module_tree_info == null)
				return;

			// find module parent
			if (module_tree_info.Parent != null)
				module_tree_info = (ExpansionSetupTreeInfo)module_tree_info.Parent;

			// do not remove main settings
			if (module_tree_info.ModuleIndex == 0)
				return;

      SettingsFile.Editing.ModuleRemove(module_tree_info.ModuleIndex - 1);

			// remove module
			m_current_expansion_manager.SetupRemoveModule(module_tree_info.ModuleIndex);
		}
	}
}
