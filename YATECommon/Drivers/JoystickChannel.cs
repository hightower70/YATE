using System.ComponentModel;

namespace YATECommon.Drivers
{
  public enum JoystickChannel
  {
    Undefined,

    [Description("X+")]
    XP,
    [Description("X-")]
    XM,

    [Description("Y+")]
    YP,
    [Description("Y-")]
    YM,

    [Description("Z+")]
    ZP,
    [Description("Z-")]
    ZM,


    [Description("R+")]
    RP,
    [Description("R-")]
    RM,

    [Description("U+")]
    UP,
    [Description("U-")]
    UM,

    [Description("V+")]
    VP,
    [Description("V-")]
    VM,

    Button1,
    Button2,
    Button3,
    Button4,
    Button5,
    Button6,
    Button7,
    Button8,
    Button9,
    Button10,
    Button11,
    Button12,
    Button13,
    Button14,
    Button15,
    Button16
  }
}
