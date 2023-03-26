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
    SystemROM,

    Video,

    [Description("Extension ROM & Slot 0")]
    ExtSlot0,
    [Description("Extension ROM & Slot 1")]
    ExtSlot1,
    [Description("Extension ROM & Slot 2")]
    ExtSlot2,
    [Description("Extension ROM & Slot 3")]
    ExtSlot3
  }
}
