using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon;

namespace YATE.Managers
{
  public class BreakpointInfo : INotifyPropertyChanged
  {
    public TVCMemoryType MemoryType { get; set; }
    public ushort Address { get; set; }

    public BreakpointInfo(TVCMemoryType in_memory_type, ushort in_address)
    {
      MemoryType = in_memory_type;
      Address = in_address;
    }

    public override bool Equals(object obj)
    {
      BreakpointInfo breakpoint_info = obj as BreakpointInfo;
      if (breakpoint_info != null)
      {
        return (MemoryType == breakpoint_info.MemoryType && Address == breakpoint_info.Address);
      }

      return false;
    }

    public override string ToString()
    {
      return string.Format("0x{0:X4} ({1})", Address, MemoryType.ToString());
    }

    public override int GetHashCode()
    {
      int hashcode = 1430287;
      hashcode = hashcode * 7302013 ^ Address.GetHashCode();
      hashcode = hashcode * 7302013 ^ MemoryType.GetHashCode();
      return hashcode;
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
