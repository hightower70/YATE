using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon;
using YATECommon.Helpers;

namespace YATE.Managers
{
  public class BreakpointInfo : INotifyPropertyChanged
  {
    public TVCMemoryType MemoryType { get; set; }
    public ushort Address { get; set; }
    public ushort Page { get; set; }

    public BreakpointInfo(TVCMemoryType in_memory_type, ushort in_address, ushort in_page = 0)
    {
      MemoryType = in_memory_type;
      Address = in_address;
      Page = in_page;
    }

    public override bool Equals(object obj)
    {
      BreakpointInfo breakpoint_info = obj as BreakpointInfo;
      if (breakpoint_info != null)
      {
        return (MemoryType == breakpoint_info.MemoryType && Address == breakpoint_info.Address && Page == breakpoint_info.Page);
      }

      return false;
    }

    public override string ToString()
    {
      return string.Format("0x{0:X4} ({1},0x{2:X4})", Address, MemoryType.ToString(),Page);
    }

    public override int GetHashCode()
    {
      return HashCodeHelper.Hash<ushort, TVCMemoryType, ushort>(Address, MemoryType, Page);
    }

    #region · INotifyPropertyHandler ·

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      if (PropertyChanged != null && propertyName != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
