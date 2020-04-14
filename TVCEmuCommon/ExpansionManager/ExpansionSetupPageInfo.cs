///////////////////////////////////////////////////////////////////////////////
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
// Setup page information
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Windows;
using TVCEmuCommon.Settings;

namespace TVCEmuCommon.ExpansionManager
{
  /// <summary>
  /// Class for storing setup page configuration
  /// </summary>
	public class ExpansionSetupPageInfo
	{
    private FrameworkElement m_form;

    /// <summary>
    /// Construct setup page
    /// </summary>
    /// <param name="in_page_name">Page name</param>
    /// <param name="in_form">Page GUI form</param>
    /// <param name="in_data_provider">Data provider for the setup data</param>
		public ExpansionSetupPageInfo(string in_page_name, Type in_page_class_type)
		{
			PageName = in_page_name;
      PageClassType = in_page_class_type;
			DataProvider = null;
      m_form = null;
		}

    /// <summary>Name of the page (shown in the setup tree)</summary>
		public string PageName;

    /// <summary>GUI form for this setup page</summary>
		public FrameworkElement Form
    {
      get
      {
        if (m_form == null)
          m_form = (FrameworkElement)Activator.CreateInstance(PageClassType);

        return m_form;
      }
    }

    public Type PageClassType { get; private set; }

    /// <summary>Data provider for this setup page</summary>
		public ISettingsDataProvider DataProvider;
	}
}
