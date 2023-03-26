using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATECommon;

namespace YATE.Managers
{
  public class BreakpointManager : IBreakpointManager, INotifyPropertyChanged
  {
    public enum BreakpointChangedMode
    {
      Added,
      Deleted
    };

    public delegate void BreakpointChangedEventHandler(BreakpointManager in_sender, BreakpointChangedMode in_mode, BreakpointInfo in_breakpoint);


    public event BreakpointChangedEventHandler BreakpointChanged;

    private HashSet<ushort> m_breakpoint_addresses;


    #region · Properties ·
    public ObservableCollection<BreakpointInfo> Breakpoints { get; private set; }
    public bool IsBreakpointsExists { get; private set; }

    #endregion

    #region · Singleton members ·
    private static BreakpointManager m_default;
    public static BreakpointManager Default
    {
      get
      {
        if (m_default == null)
        {
          m_default = new BreakpointManager();
        }
        return m_default;
      }
    }
    #endregion

    public BreakpointManager()
    {
      Breakpoints = new ObservableCollection<BreakpointInfo>();
      m_breakpoint_addresses = new HashSet<ushort>();
    }

    public void AddBreakpoint(BreakpointInfo in_breakpoint)
    {
      // check if breakpoint already exists
      if (Breakpoints.IndexOf(in_breakpoint) < 0)
      {
        Breakpoints.Add(in_breakpoint);
        UpdateBreakpointList();
        OnBreakpointChanged(BreakpointChangedMode.Added, in_breakpoint);
      }
    }

    public void RemoveBreakpoint(BreakpointInfo in_breakpoint)
    {
      // check if breakpoint exists
      int breakpoint_index = Breakpoints.IndexOf(in_breakpoint);
      if (breakpoint_index >= 0)
      {
        Breakpoints.RemoveAt(breakpoint_index);
        UpdateBreakpointList();
        OnBreakpointChanged(BreakpointChangedMode.Deleted, in_breakpoint);
      }
    }

    private void UpdateBreakpointList()
    {
      // update breakpoint existance flag
      IsBreakpointsExists = Breakpoints.Count > 0;

      // update address list
      m_breakpoint_addresses.Clear();

      for (int i = 0; i < Breakpoints.Count; i++)
      {
        m_breakpoint_addresses.Add(Breakpoints[i].Address);
      }
    }

    private void OnBreakpointChanged(BreakpointChangedMode in_mode, BreakpointInfo in_breakpoint)
    {
      BreakpointChanged?.Invoke(this, in_mode, in_breakpoint);
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
