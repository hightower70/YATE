using System.Xml;

namespace TVCEmuCommon.Settings
{
  public class ModuleBaseSettingsInfo
  {
    public string SectionName = "";
    public string ModuleName = "";
    public bool Active = false;
    public int ModuleIndex = -1;
    public int SlotIndex = -1;

    public ModuleBaseSettingsInfo()
    {
    }

    public ModuleBaseSettingsInfo(string in_section_name)
    {
      SectionName = in_section_name;
    }

    public ModuleBaseSettingsInfo(string in_secion_name, string in_module_name, bool in_active)
    {
      SectionName = in_secion_name;
      ModuleName = in_module_name;
      Active = in_active;
    }

    public ModuleBaseSettingsInfo(string in_secion_name, string in_module_name, bool in_active, int in_slot_index)
    {
      SectionName = in_secion_name;
      ModuleName = in_module_name;
      Active = in_active;
      ModuleIndex = 0;
      SlotIndex = in_slot_index;
    }

    public ModuleBaseSettingsInfo(ModuleBaseSettingsInfo in_module_info)
    {
      SectionName = in_module_info.SectionName;
      ModuleName = in_module_info.ModuleName;
      Active = in_module_info.Active;
      ModuleIndex = 0;
      SlotIndex = in_module_info.SlotIndex;
    }

    public ModuleBaseSettingsInfo(XmlNode in_node)
    {
      Load(in_node);
    }

    public void Load(XmlNode in_node)
    {
      SectionName = in_node.Name;
      ModuleName = in_node.Attributes["ModuleName"].InnerText;
      bool.TryParse(in_node.Attributes["Active"].InnerText, out Active);
                               /*
      if (in_node.Attributes["ModuleIndex"] != null)
        int.TryParse(in_node.Attributes["ModuleIndex"].InnerText, out ModuleIndex);

      if (in_node.Attributes["SlotIndex"] != null)
        int.TryParse(in_node.Attributes["SlotIndex"].InnerText, out SlotIndex);        */
    }

    public void Save(XmlNode in_node)
    {
      // create 'ModuleName' attribute
      XmlAttribute module_name = in_node.OwnerDocument.CreateAttribute("ModuleName");
      module_name.Value = ModuleName;
      in_node.Attributes.Append(module_name);

      // create 'Active' attribute
      XmlAttribute active = in_node.OwnerDocument.CreateAttribute("Active");
      active.Value = Active.ToString();
      in_node.Attributes.Append(active);
                 /*
      // create 'SlotIndex' attribute
      if (SlotIndex >= 0)
      {
        XmlAttribute slot_index = in_node.OwnerDocument.CreateAttribute("SlotIndex");
        slot_index.Value = SlotIndex.ToString();
        in_node.Attributes.Append(slot_index);
      }

      // create 'ModuleIndex' attribute
      if (ModuleIndex >= 0)
      {
        XmlAttribute slot_index = in_node.OwnerDocument.CreateAttribute("ModuleIndex");
        slot_index.Value = SlotIndex.ToString();
        in_node.Attributes.Append(slot_index);
      }         */
    }

    public override string ToString()
    {
      return SectionName;
    }

    public override bool Equals(object in_object)
    {
      // If this and obj do not refer to the same type, then they are not equal.
      if (in_object.GetType() != this.GetType())
        return false;

      return (SectionName == ((ModuleBaseSettingsInfo)in_object).SectionName);
    }

    public override int GetHashCode()
    {
      return SectionName.GetHashCode();
    }
  }
}
