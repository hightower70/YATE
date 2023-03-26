using YATECommon.Settings;

namespace YATE.Settings
{
  public class DisassemblyListViewSettings : IndexedEmulatorSettingsBase
  {
    public TVCMemorySelectorSettings MemorySelection;

    public DisassemblyListViewSettings() : base("DisassemblyListView")
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
