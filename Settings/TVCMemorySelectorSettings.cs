using YATECommon;

namespace YATE.Settings
{
  public class TVCMemorySelectorSettings
  {
    public TVCMemoryType MemoryType { get; set; }

    public int PageIndex { get; set; }

    public string StartAddress { get; set; }

    public string EndAddress { get; set; }


    public void SetDefault()
    {
      MemoryType = TVCMemoryType.RAM;

      PageIndex = 0;

      StartAddress = "0";

      EndAddress = "0xffff";
    }
  }
}
