using System.ComponentModel;

namespace YATECommon
{
  public enum TVCMemoryType
  {
    [Description("RAM Memory")]
    RAM,

    [Description("Cart")]
    Cart,

    [Description("Basic ROM")]
    BasicROM,

    [Description("Extension ROM")]
    ExtROM,

    Slot0,
    Slot1,
    Slot2,
    Slot3
  }
}
