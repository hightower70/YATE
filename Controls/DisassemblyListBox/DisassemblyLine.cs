using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YATE.Emulator.Z80CPU;

namespace YATE.Controls
{
  public class DisassemblyLine : IComparable<DisassemblyLine>, INotifyPropertyChanged
  {
    private bool m_is_breakpoint;
    private bool m_is_executing;

    public enum DisassemblyLineType
    {
      Disassembly,
      Label,
      Data
    }

    public DisassemblyLineType Type { get; set; }

    public bool IsBreakpoint
    {
      get
      {
        return m_is_breakpoint;
      }
      set
      {
        m_is_breakpoint = value;
        OnPropertyChanged();
      }
    }

    public bool IsExecuting
    {
      get
      {
        return m_is_executing;
      }
      set
      {
        m_is_executing = value;
        OnPropertyChanged();
      }
    }

    public Z80DisassemblerInstruction DisassemblyInstruction { get; set; }

    public DisassemblyLine()
    {
      Type = DisassemblyLineType.Disassembly;
      DisassemblyInstruction = new Z80DisassemblerInstruction();
    }

    public DisassemblyLine(Z80DisassemblerInstruction in_instruction)
    {
      Type = DisassemblyLineType.Disassembly;
      DisassemblyInstruction = in_instruction;
    }

    public int CompareTo(DisassemblyLine other)
    {
      return DisassemblyInstruction.Address.CompareTo(other.DisassemblyInstruction.Address);
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
