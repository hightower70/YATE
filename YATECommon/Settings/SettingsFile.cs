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
// System settings handler class
///////////////////////////////////////////////////////////////////////////////
using CustomControls.Utils;
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
using System.Xml.Serialization;

namespace YATECommon.Settings
{
  public class SettingsFile
  {
    #region · Constants ·
    private const string RootElementName = "Settings";
    private const string EmulatorSettingsElementName = "Emulator";
    private const string TVCSettingsElementName = "TVC";

    public const string ExpanstionActiveAttribute = "Active";
    public const string ExpansionIndexAtribute = "ExpansionIndex";
    #endregion

    #region · Types ·

    #endregion

    #region · Data members ·
    private XmlDocument m_xml_doc = null;
    #endregion

    #region · Constructor ·
    // Default constructor
    public SettingsFile()
    {
      m_xml_doc = new XmlDocument();
    }
    #endregion

    #region · Properties ·

    /// <summary>
    /// Name of the current configuration file
    /// </summary>
    public string ConfigFileName { get; set; }

    #endregion

    #region · Public members ·

    /// <summary>
    /// Adds content of a settings class to the settings file
    /// </summary>
    /// <param name="in_settings_data"></param>
    public void SetSettings(SettingsBase in_settings_data, int in_expansion_index = -1)
    {
      XmlNode parent_node = null;
      XmlNode settings_node = null;

      // select settings node
      switch (in_settings_data.Category)
      {
        case SettingsBase.SettingsCategory.Emulator:
          parent_node = m_xml_doc.DocumentElement.SelectSingleNode('/' + RootElementName + '/' + EmulatorSettingsElementName);
          settings_node = parent_node.SelectSingleNode(in_settings_data.ModuleName);
          break;

        case SettingsBase.SettingsCategory.TVC:
          parent_node = m_xml_doc.DocumentElement.SelectSingleNode('/' + RootElementName + '/' + TVCSettingsElementName + "[@Active = 'true']");
          if (in_settings_data is ExpansionSettingsBase)
          {
            settings_node = parent_node.SelectSingleNode(in_settings_data.ModuleName + "[@ExpansionIndex = '" + ((ExpansionSettingsBase)in_settings_data).ExpansionIndex.ToString() + "']");
          }
          else
          {
            settings_node = parent_node.SelectSingleNode(in_settings_data.ModuleName);
          }
          break;
      }

      if(settings_node != null)
      {
        UpdateEntry(settings_node, in_settings_data);
      }
      else
      {
        SerializeEntry(parent_node, in_settings_data);
      }



      /*
      string key = GetSettigsKey(in_settings_data);

      // replace setings to the new class
      if (m_settings.ContainsKey(key))
        m_settings.Remove(key);

      m_settings.Add(key, in_settings_data);*/
    }

    /// <summary>
    /// Gets settings class
    /// </summary>
    /// <param name="in_settings_data"></param>
    /// <returns></returns>
    public T GetSettings<T>(int in_expansion_index = -1) where T : SettingsBase, new()
    {
      XmlNode root_node = null;
      bool success = true;
      XmlNode settings_node = null;
      T result = new T();

      // check if the settings exists in the XML
      if (m_xml_doc == null)
        success = false;

      // check XML file
      if (success)
      {
        root_node = m_xml_doc.DocumentElement;

        if (root_node == null || root_node.Name != RootElementName)
          success = false;
      }

      // select settings node
      if (success)
      {
        switch(result.Category)
        {
          case SettingsBase.SettingsCategory.Emulator:
            settings_node = root_node.SelectSingleNode('/' + RootElementName + '/' + EmulatorSettingsElementName + '/' + result.ModuleName);
            break;

          case SettingsBase.SettingsCategory.TVC:
            if (result is ExpansionSettingsBase)
            {
              settings_node = root_node.SelectSingleNode('/' + RootElementName + '/' + TVCSettingsElementName + "[@Active = 'true']" + '/' + result.ModuleName + "[@ExpansionIndex = '" + in_expansion_index.ToString() + "']");
            }
            else
            {
              settings_node = root_node.SelectSingleNode('/' + RootElementName + '/' + TVCSettingsElementName + "[@Active = 'true']" + '/' + result.ModuleName);
            }
            break;
        }

        if (settings_node == null)
          success = false;
      }

      // deserialize settings data
      if (success)
      {
        DeserializeEntry(settings_node, result);
      }

      // store settings or set to default values
      if (!success)
        result.SetDefaultValues();

      return result;
    }

    /// <summary>
    /// Generates the list of the modules stored in config file
    /// </summary>
    /// <returns>List of the modules</returns>
    public List<ExpansionSettingsBase> GetExpansionList()
    {
      List<ExpansionSettingsBase> expansions = new List<ExpansionSettingsBase>();

      // check if the settings exists in the XML
      if (m_xml_doc != null)
      {
        XmlElement root_node = m_xml_doc.DocumentElement;

        if (root_node != null && root_node.Name == RootElementName)
        {
          XmlNodeList modules_node = root_node.SelectNodes('/' + RootElementName + '/' + TVCSettingsElementName + "[@Active = 'true']" + "/*");

          for (int i = 0; i < modules_node.Count; i++)
          {
            // try to deserialize card
            CardSettingsBase card_settings = new CardSettingsBase(SettingsBase.SettingsCategory.TVC, modules_node[i].Name);

            DeserializeEntry(modules_node[i], card_settings);

            if (card_settings.ExpansionIndex >= 0 && card_settings.SlotIndex >= 0)
            {
              if (card_settings.Active)
                expansions.Add(card_settings);
            }
            else
            {
              // this is not a card it's an expansion
              ExpansionSettingsBase settings_base = new ExpansionSettingsBase(SettingsBase.SettingsCategory.TVC, modules_node[i].Name);

              DeserializeEntry(modules_node[i], settings_base);

              if (settings_base.ExpansionIndex >= 0)
              {
                if (settings_base.Active)
                  expansions.Add(settings_base);
              }
            }
          }
        }
      }

      return expansions;
    }

    /// <summary>
    /// Generates the list of the expansion cards stored in config file
    /// </summary>
    /// <returns>List of the modules</returns>
    public List<CardSettingsBase> GetCardList()
    {
      List<CardSettingsBase> cards = new List<CardSettingsBase>();

      // check if the settings exists in the XML
      if (m_xml_doc != null)
      {
        XmlElement root_node = m_xml_doc.DocumentElement;

        if (root_node != null && root_node.Name == RootElementName)
        {
          XmlNodeList modules_node = root_node.SelectNodes('/' + RootElementName + '/' + TVCSettingsElementName + "[@Active = 'true']" + "/*");

          for (int i = 0; i < modules_node.Count; i++)
          {
            CardSettingsBase settings_base = new CardSettingsBase(SettingsBase.SettingsCategory.TVC, modules_node[i].Name);

            DeserializeEntry(modules_node[i], settings_base);

            if (settings_base.ExpansionIndex >= 0 && settings_base.SlotIndex >= 0 && settings_base.Active)
              cards.Add(settings_base);
          }
        }
      }

      return cards;
    }

    public void ModuleAdd(ExpansionSettingsBase inout_module_info)
    {
      XmlNode root_node = m_xml_doc.DocumentElement;
      XmlNode parent_node = root_node.SelectSingleNode('/' + RootElementName + '/' + TVCSettingsElementName + "[@Active = 'true']");
      XmlNodeList expansion_node = parent_node.SelectNodes(inout_module_info.ModuleName);
      bool create_new = true;

      inout_module_info.ExpansionIndex = 0;
      ExpansionSettingsBase expansion_info = (ExpansionSettingsBase)Activator.CreateInstance(inout_module_info.GetType(), SettingsBase.SettingsCategory.TVC, inout_module_info.ModuleName);
      for (int i = 0; i < expansion_node.Count; i++)
      {
        DeserializeEntry(expansion_node[i], expansion_info);

        if (!expansion_info.Active)
        {
          // if there is an inactive expansion, then activate it
          inout_module_info.ExpansionIndex = expansion_info.ExpansionIndex;
          create_new = false;

          // update existing node
          UpdateEntry(expansion_node[i], inout_module_info);

          break;
        }
        else
        {
          if (inout_module_info.ExpansionIndex <= expansion_info.ExpansionIndex)
            inout_module_info.ExpansionIndex = expansion_info.ExpansionIndex + 1;
        }
      }

      if(create_new)
      {
        // create a new node
        SerializeEntry(parent_node, inout_module_info);
      }
    }

		public void ModuleActivate(ExpansionSettingsBase in_module)
		{
			try
			{
				XmlNode root = m_xml_doc.DocumentElement;
				XmlNodeList node_list = root.SelectNodes('/' + in_module.ModuleName + "/*");

				if(in_module.ExpansionIndex < node_list.Count)
				{
					node_list[in_module.ExpansionIndex].Attributes[ExpanstionActiveAttribute].Value = "true";
				}
			}
			catch
			{
				// error occured
			}

		}


    /// <summary>
    /// Module Deactivate
    /// </summary>
    /// <param name="in_module"></param>
    public void ModuleDeactivate(string in_expansion_name, int in_expansion_index)
    {
      try
      {
        XmlNode root_node = m_xml_doc.DocumentElement;
        XmlNode parent_node = root_node.SelectSingleNode('/' + RootElementName + '/' + TVCSettingsElementName + "[@Active = 'true']");
        XmlNode module_node = parent_node.SelectSingleNode(in_expansion_name + "[@ExpansionIndex = '" + in_expansion_index.ToString() + "']");

        if (module_node != null)
        {
          ExpansionSettingsBase settings = new ExpansionSettingsBase(SettingsBase.SettingsCategory.TVC, in_expansion_name);
          DeserializeEntry(module_node, settings);

          settings.Active = false;
          UpdateEntry(module_node, settings);
        }
      }
      catch
      {
        // error occured
      }

    }


    /// <summary>
    /// Loads settings file
    /// </summary>
    public bool Load()
    {
      bool success = true;

      // get file name
      ConfigFileName = GetConfigFileName();

      try
      {
        // load XML settings
        m_xml_doc = new XmlDocument();

        m_xml_doc.Load(ConfigFileName);
      }
      catch
      {
        m_xml_doc = null;

        success = false;
      }

      // if loading was unsuccessfull create empty xml document
      if (!success)
      {
        // error occured -> create empty xml
        m_xml_doc = new XmlDocument();

        XmlNode xml_declaration = m_xml_doc.CreateXmlDeclaration("1.0", "UTF-8", "");
        m_xml_doc.AppendChild(xml_declaration);

        XmlNode top_level_parent_node = m_xml_doc.CreateElement(RootElementName);
        m_xml_doc.AppendChild(top_level_parent_node);

        XmlNode main_settings = m_xml_doc.CreateElement(EmulatorSettingsElementName);
        top_level_parent_node.AppendChild(main_settings);

        XmlNode tvc_settings = m_xml_doc.CreateElement(TVCSettingsElementName);
        XmlAttribute attr = m_xml_doc.CreateAttribute("Active");
        attr.Value = "true";
        tvc_settings.Attributes.Append(attr);
        top_level_parent_node.AppendChild(main_settings);
      }

      return success;
    }

    /// <summary>
    /// Saves settings file
    /// </summary>
    public void Save()
    {
      // save XML
      m_xml_doc.Save(ConfigFileName);
    }

    /// <summary>
    /// Copies settings from another settings class
    /// </summary>
    /// <param name="in_settings"></param>
    public void CopySettingsFrom(SettingsFile in_settings)
    {
      // clone XML doc
      m_xml_doc = (XmlDocument)in_settings.m_xml_doc.Clone();
    }

    #endregion

    #region · Singleton members ·

    private static SettingsFile m_default = null;
    private static SettingsFile m_editing = null;

    /// <summary>
    /// Gets default singleton instance
    /// </summary>
    public static SettingsFile Default
    {
      get
      {
        if (m_default == null)
        {
          m_default = new SettingsFile();
        }

        return m_default;
      }
    }

    /// <summary>
    /// Gets instance of the currently editing settings
    /// </summary>
    public static SettingsFile Editing
    {
      get
      {
        if (m_editing == null)
        {
          m_editing = new SettingsFile();
        }

        return m_editing;
      }
    }
    #endregion

    #region · Private members ·

    /// <summary>
    /// Gets object's member value based on member info
    /// </summary>
    /// <param name="in_info"></param>
    /// <param name="in_object"></param>
    /// <returns></returns>
    private object GetObjectMember(MemberInfo in_info, object in_object)
    {
      switch (in_info.MemberType)
      {
        case MemberTypes.Field:
          return ((FieldInfo)in_info).GetValue(in_object);

        case MemberTypes.Property:
          return ((PropertyInfo)in_info).GetValue(in_object, null);
      }

      return null;
    }

    /// <summary>
    /// Sets object"s member value
    /// </summary>
    /// <param name="in_info"></param>
    /// <param name="in_object"></param>
    /// <param name="in_value"></param>
    /// <param name="in_index"></param>
    private void SetObjectMember(MemberInfo in_info, object in_object, object in_value, int? in_index = null)
    {
      // check if array element
      if (in_index != null)
      {
        // set array element
        Type type = ((Array)in_object).GetType().GetElementType();

        ((Array)in_object).SetValue(Convert.ChangeType(in_value, type, CultureInfo.InvariantCulture), (int)in_index);
      }
      else
      {
        // set if field
        if (in_info is FieldInfo)
        {
          if (((FieldInfo)in_info).FieldType.IsEnum)
          {
            ((FieldInfo)in_info).SetValue(in_object, Enum.Parse(((FieldInfo)in_info).FieldType, in_value as string));
          }
          else
          {
            ((FieldInfo)in_info).SetValue(in_object, Convert.ChangeType(in_value, ((FieldInfo)in_info).FieldType, CultureInfo.InvariantCulture));
          }
        }
        else
        {
          // set if property
          if (in_info is PropertyInfo)
          {
            if (((PropertyInfo)in_info).PropertyType.Name == "IPAddress")
            {
              ((PropertyInfo)in_info).SetValue(in_object, IPAddress.Parse(in_value as string));
            }
            else
            {
              if (((PropertyInfo)in_info).PropertyType.IsEnum)
              {
                ((PropertyInfo)in_info).SetValue(in_object, Enum.Parse(((PropertyInfo)in_info).PropertyType, in_value as string));
              }
              else
              {
                ((PropertyInfo)in_info).SetValue(in_object, Convert.ChangeType(in_value, ((PropertyInfo)in_info).PropertyType, CultureInfo.InvariantCulture), null);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Deserializes enty
    /// </summary>
    /// <param name="in_retval"></param>
    private void DeserializeEntry(XmlNode in_node, object in_object, int? in_index = null)
    {
      XmlNode node;
      Type type = in_object.GetType();

      MemberInfo[] member_info = GetSerializableMembers(type);

      // deserialize all members
      foreach (MemberInfo member in member_info)
      {
        object value;

        value = GetObjectMember(member, in_object);
        if (value != null)
        {
          if (value.GetType().IsPrimitive || value is string || value is Enum || value is IPAddress)
          {
            // check for attribute 
            if (member.GetCustomAttribute(typeof(XmlAttributeAttribute), true) != null)
            {
              XmlAttribute attribute = in_node.Attributes[member.Name];

              if (attribute != null)
                SetObjectMember(member, in_object, attribute.Value);
            }
            else
            {
              node = in_node.SelectSingleNode(member.Name);
              if (node != null)
                SetObjectMember(member, in_object, node.InnerText);
            }
          }
          else
          {
            node = in_node.SelectSingleNode(member.Name);
            if (node != null)
              DeserializeEntry(node, value);
          }
        }
      }
    }

    /// <summary>
    /// Gets settings class members to serialize
    /// </summary>
    /// <param name="in_type"></param>
    /// <returns></returns>
    protected MemberInfo[] GetSerializableMembers(Type in_type)
    {
      MemberInfo[] member_info;

      if (in_type.IsSerializable)
      {
        // if 'Serializable' attribute is defined -> select all 'serializable' members
        member_info = FormatterServices.GetSerializableMembers(in_type);
      }
      else
      {
        // if 'DataContract' attribute is specified -> select all members which has 'DataMember' attribute
        DataContractAttribute[] attributes = (DataContractAttribute[])in_type.GetCustomAttributes(typeof(DataContractAttribute), true);
        if (attributes != null && attributes.Length > 0)
        {
          // get members with 'DataMember' attribute
          MemberInfo[] field_info = in_type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.GetField).Where(p => p.GetCustomAttributes(typeof(DataMemberAttribute), true).Count() != 0).ToArray();
          MemberInfo[] property_info = in_type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty).Where(p => p.GetCustomAttributes(typeof(DataMemberAttribute), true).Count() != 0).ToArray();

          member_info = field_info.Concat(property_info).ToArray();
        }
        else
        {
          // no attribute specified -> select public members
          member_info = in_type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField | BindingFlags.GetField).Where(p => p.GetCustomAttributes(typeof(NonSerializedAttribute), true).Count() == 0).ToArray();
          member_info = member_info.Concat(in_type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty)).ToArray().Where(p => p.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Count() == 0).ToArray();
        }
      }

      // retrun member info
      return member_info;
    }

    /// <summary>
    /// Writes entry to the binary file and recursivelly calls itself for all elements of a collection types
    /// </summary>
    /// <param name="in_object"></param>
    /// <param name="in_name"></param>
    private void SerializeEntry(XmlNode in_parent, object in_object, MemberInfo in_member_info = null)
    {
      XmlNode child = null;

      if (in_object == null)
      {
        WriteValue(in_parent, in_member_info, "null");
      }
      else if (in_object is sbyte || in_object is byte || in_object is short || in_object is ushort || in_object is int || in_object is uint || in_object is long || in_object is ulong || in_object is decimal || in_object is double || in_object is float)
      {
        WriteValue(in_parent, in_member_info, Convert.ToString(in_object, NumberFormatInfo.InvariantInfo));
      }
      else if (in_object is bool)
      {
        WriteValue(in_parent, in_member_info, in_object.ToString().ToLower());
      }
      else if (in_object is char || in_object is Enum || in_object is Guid)
      {
        WriteValue(in_parent, in_member_info, "" + in_object);
      }
      else if (in_object is DateTime)
      {
        WriteValue(in_parent, in_member_info, ((DateTime)in_object - new DateTime(1970, 1, 1)).TotalMilliseconds.ToString("0"));
      }
      else if (in_object is string)
      {
        WriteValue(in_parent, in_member_info, (string)in_object);
      }
      else if (in_object is IPAddress)
      {
        WriteValue(in_parent, in_member_info, ((IPAddress)in_object).ToString());
      }
      else if (in_object is Array)
      {
        // get item type
        string type_name = in_object.GetType().FullName;

        // create array element
        child = CreateXMLNode(type_name, in_member_info.Name);
        in_parent.AppendChild(child);

        // deserialize array elements
        foreach (object obj in (in_object as Array))
        {
          SerializeEntry(child, obj);
        }
      }
      else
      {
        // get item name
        string type_name = in_object.GetType().FullName;

        // determine element name
        string name = null;
        if (in_member_info != null)
          name = in_member_info.Name;
        else
        {
          if (in_object is SettingsBase)
          {
            name = ((SettingsBase)in_object).ModuleName;
          }
        }

        // create or modify child element
        child = in_parent.SelectSingleNode(name);
        if (child == null)
        {
          child = CreateXMLNode(type_name, name);
          in_parent.AppendChild(child);
        }

        // store fields fields
        MemberInfo[] members = GetSerializableMembers(in_object.GetType());
        foreach (MemberInfo member in members)
        {
          switch (member.MemberType)
          {
            case MemberTypes.Field:
              SerializeEntry(child, ((FieldInfo)member).GetValue(in_object), member);
              break;

            case MemberTypes.Property:
              SerializeEntry(child, ((PropertyInfo)member).GetValue(in_object, null), member);
              break;
          }
        }
      }
    }


    private void UpdateEntry(XmlNode in_node, object in_object)
    {
      // store fields fields
      MemberInfo[] members = GetSerializableMembers(in_object.GetType());
      foreach (MemberInfo member in members)
      {
        switch (member.MemberType)
        {
          case MemberTypes.Field:
            SerializeEntry(in_node, ((FieldInfo)member).GetValue(in_object), member);
            break;

          case MemberTypes.Property:
            SerializeEntry(in_node, ((PropertyInfo)member).GetValue(in_object, null), member);
            break;
        }
      }
    }

    /// <summary>
    /// Creates an XML node
    /// </summary>
    /// <param name="in_type">Type of the node (stored as an attribute)</param>
    /// <param name="in_name">Name of the node</param>
    /// <returns>The newly created node</returns>
    private XmlNode CreateXMLNode(string in_type, string in_name)
    {
      XmlNode node;
      string element_name;

      // if name is not specified use type
      if (string.IsNullOrEmpty(in_name))
      {

        int pos = in_type.LastIndexOf('+');

        if (pos != -1)
        {
          element_name = in_type.Substring(pos + 1, in_type.Length - pos - 1);
        }
        else
        {
          element_name = in_type;
        }
      }
      else
      {
        element_name = in_name;
      }

      // create node
      node = m_xml_doc.CreateNode(XmlNodeType.Element, element_name, null);

      // append type as an attribute
      if (!string.IsNullOrEmpty(in_type))
      {
        //((XmlElement)node).SetAttribute(XMLTypeAttributeName, in_type);
      }

      return node;
    }

    /// <summary>
    /// Writes value to the output stream
    /// </summary>
    /// <param name="in_type">Type string</param>
    /// <param name="in_member_info">Member onformation</param>
    /// <param name="in_value">Value string</param>
    private void WriteValue(XmlNode in_parent, MemberInfo in_member_info, string in_value)
    {
      XmlNode child = null;

      // check for attribute 
      if (in_member_info.GetCustomAttribute(typeof(XmlAttributeAttribute), true) != null)
      {
        XmlAttribute attribute = in_parent.Attributes[in_member_info.Name];

        if (attribute == null)
        {
          attribute = m_xml_doc.CreateAttribute(in_member_info.Name);
          in_parent.Attributes.Append(attribute);
        }

        attribute.Value = in_value;
      }
      else
      {
        child = in_parent.SelectSingleNode(in_member_info.Name);

        if (child == null)
        {
          // create value element
          child = in_parent.OwnerDocument.CreateNode(XmlNodeType.Element, in_member_info.Name, null);
          in_parent.AppendChild(child);
        }

        child.InnerText = in_value;
      }
    }


    /// <summary>
    /// Gets configuration file name
    /// </summary>
    /// <returns>File name</returns>
    static public string GetConfigFileName()
    {
      string application_name = SystemPaths.GetExecutableName();
      RegistryKey software = Registry.CurrentUser.OpenSubKey("Software");
      RegistryKey application_name_key = software.OpenSubKey(application_name);

      if (application_name_key != null && application_name_key.GetValue("SettingsFile") != null)
        return application_name_key.GetValue("SettingsFile").ToString();
      else
      {
        string application_directory = SystemPaths.GetApplicationDataPath();

        return Path.Combine(application_directory, application_name + ".config");
      }
    }
    #endregion
  }
}

