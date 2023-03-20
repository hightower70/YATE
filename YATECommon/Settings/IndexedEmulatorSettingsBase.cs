using System.Xml.Serialization;

namespace YATECommon.Settings
{
  public class IndexedEmulatorSettingsBase : SettingsBase
  {
    [XmlAttribute]
    public int Index { get; set; }

    public IndexedEmulatorSettingsBase(string in_expansion_name) : base(SettingsCategory.EmulatorIndexed, in_expansion_name)
    {
      Index = 0;
    }
  }
}
