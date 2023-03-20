using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YATECommon.Settings;
using static YATECommon.Settings.SettingsBase;

namespace YATE.Settings
{
  public class HexViewSettings : IndexedEmulatorSettingsBase
  {
    public TVCMemorySelectorSettings MemorySelection;

    public HexViewSettings() : base("HexView")
    {
      MemorySelection = new TVCMemorySelectorSettings();
      SetDefaultValues();
    }

    override public void SetDefaultValues()
    {
      MemorySelection.SetDefault();
    }
  }
}
