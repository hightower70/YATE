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
// System settings handler class
///////////////////////////////////////////////////////////////////////////////
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using TVCEmuCommon.Helpers;

namespace CommonClassLibrary.Settings
{
	public interface ISettingsFileBase
	{
		void SetSettings(SettingsBase in_settings_data);
		T GetSettings<T>() where T : SettingsBase, new();
									/*
		List<ModuleInfo> GetModuleList();
		void ModuleAdd(ModuleInfo in_module_info);
		void ModuleRemove(int in_module_index);
								*/
		bool Load();
		void Save();
		void CopySettingsFrom(SettingsFileBase in_settings);
		
	}
}

