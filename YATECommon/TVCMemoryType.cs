using System.ComponentModel;

namespace YATECommon
{
  public enum TVCMemoryType
  {
    [Description("RAM Memory")]
    RAM,

    [Description("Cart")]
    Cart,

    [Description("System ROM")]
    System,

    Video,

    [Description("Extension ROM")]
    SystemExtension,

    [Description("Slot 0 ROM")]
    Slot0,
    [Description("Slot 1 ROM")]
    Slot1,
    [Description("Slot 2 ROM")]
    Slot2,
    [Description("Slot 3 ROM")]
    Slot3
  }
}
