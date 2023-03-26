﻿using CustomControls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YATE.Emulator.Z80CPU;
using YATE.Managers;
using YATECommon;
using static YATE.Managers.BreakpointManager;

namespace YATE.Controls
{
  public partial class DisassemblyListBox : ListBox, INotifyPropertyChanged
  {
    #region · Data members ·

    private int m_tstate_min;
    private int m_tstate_max;
    private string m_tstate_sum_string;
    private AnnotatedScrollBar m_vertical_scroll_bar;
    private TVCMemoryType m_memory_type = TVCMemoryType.Cart;

    #endregion

    #region · Properties ·

    public string TstateSumString
    {
      get
      {
        return m_tstate_sum_string;
      }
      private set
      {
        m_tstate_sum_string = value;
        OnPropertyChanged();
      }
    }
    #endregion

    #region · Constructor ·

    /// <summary>
    /// Default constructor
    /// </summary>
    public DisassemblyListBox()
    {
      m_tstate_min = 0;
      m_tstate_max = 0;
      m_tstate_sum_string = "0/0";

      SelectionMode = SelectionMode.Extended;

      BreakpointManager.Default.BreakpointChanged += BreakpointChangedEventHandler;
    }

    #endregion

    #region · Event handlers ·      

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
      base.OnMouseUp(e);

      // find which element in ListboxItem generated the event
      if(e.OriginalSource != null)
      {
        if(e.OriginalSource is FrameworkElement)
        {
          FrameworkElement element = e.OriginalSource as FrameworkElement;

          while (element != null && element != this)
          {
            // The event is generated by the address part
            if (element.Name == "PART_LineAddress")
            {
              DisassemblyLine current_line = SelectedItem as DisassemblyLine;

              if (current_line != null && current_line.Type == DisassemblyLine.DisassemblyLineType.Disassembly)
              {
                BreakpointInfo breakpoint = new BreakpointInfo(m_memory_type, current_line.DisassemblyInstruction.Address);

                if (current_line.IsBreakpoint)
                {
                  BreakpointManager.Default.RemoveBreakpoint(breakpoint);
                }
                else
                {
                  BreakpointManager.Default.AddBreakpoint(breakpoint);
                }
                break;
              }
            }

            // The click is generated by the operand1 or oeprand 2
            if(element.Name== "PART_Operand1" || element.Name == "PART_Operand2")
            {
              DisassemblyLine current_line = SelectedItem as DisassemblyLine;

              if (current_line.Type == DisassemblyLine.DisassemblyLineType.Disassembly)
              {
                // The event is only interesting when opcode is jump  or call
                if (current_line.DisassemblyInstruction.OpCode.Flags.HasFlag(Z80DisassemblerTable.OpCodeFlags.Jumps) || current_line.DisassemblyInstruction.OpCode.Flags.HasFlag(Z80DisassemblerTable.OpCodeFlags.Call))
                {
                  // if opcode has two operands only the second operand event is interesting, it it has one operand, only the event of the first operand is interesting
                  if (((string.IsNullOrEmpty(current_line.DisassemblyInstruction.Operand2) && element.Name == "PART_Operand1") ||
                       (!string.IsNullOrEmpty(current_line.DisassemblyInstruction.Operand2) && element.Name == "PART_Operand2")) &&
                      (current_line.DisassemblyInstruction.NextAddress2 != null))
                  {
                    // Change selected item
                    SelectItemAtAddress((ushort)current_line.DisassemblyInstruction.NextAddress2);
                  }
                }
              }
              break;
            }

            // try with parent if original source is not any of the known element
            element = (FrameworkElement)VisualTreeHelper.GetParent(element);

            // end of the test is ListboxItem (considered as top level parant for this event) is reached 
            if (element is ListBoxItem)
              break;
          }
        }
      }
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
      base.OnSelectionChanged(e);

      for (int i = 0; i < e.AddedItems.Count; i++)
      {
        DisassemblyLine item = (DisassemblyLine)e.AddedItems[i];

        m_tstate_min += (int)item.DisassemblyInstruction.TStates;

        if (item.DisassemblyInstruction.TStates2 > 0)
          m_tstate_max += (int)item.DisassemblyInstruction.TStates2;
        else
          m_tstate_max += (int)item.DisassemblyInstruction.TStates;
      }

      for (int i = 0; i < e.RemovedItems.Count; i++)
      {
        DisassemblyLine item = (DisassemblyLine)e.RemovedItems[i];

        m_tstate_min -= (int)item.DisassemblyInstruction.TStates;

        if (item.DisassemblyInstruction.TStates2 > 0)
          m_tstate_max -= (int)item.DisassemblyInstruction.TStates2;
        else
          m_tstate_max -= (int)item.DisassemblyInstruction.TStates;
      }

      TstateSumString = string.Format("{0}/{1}", m_tstate_min, m_tstate_max);
    }

    #endregion

    #region · Non-public members ·

    internal void SetVerticalScrollBar(AnnotatedScrollBar in_scroll_bar)
    {
      m_vertical_scroll_bar = in_scroll_bar;
    }

    private void SelectItemAtAddress(ushort in_address)
    {
      int index = FindLine(in_address);

      if (index >= 0)
      {
        SelectedIndex = index;
        ScrollIntoView(SelectedItem);
      }
    }

    private int FindLine(ushort in_address)
    {
      DisassemblyLine search = new DisassemblyLine();
      search.DisassemblyInstruction.Address = in_address;

      return (ItemsSource as List<DisassemblyLine>).BinarySearch(search);
    }

    #endregion

    private void BreakpointChangedEventHandler(BreakpointManager in_sender, BreakpointChangedMode in_mode, BreakpointInfo in_breakpoint)
    {
      switch(in_mode)
      {
        case BreakpointChangedMode.Added:
          if(in_breakpoint.MemoryType == m_memory_type)
          {
            int line_index = FindLine(in_breakpoint.Address);

            if(line_index >=0)
            {
              (ItemsSource as List<DisassemblyLine>)[line_index].IsBreakpoint = true;
              m_vertical_scroll_bar.AddAnnotation(line_index, false, Brushes.Red);
            }
          }
          break;

        case BreakpointChangedMode.Deleted:
          if (in_breakpoint.MemoryType == m_memory_type)
          {
            int line_index = FindLine(in_breakpoint.Address);

            if (line_index >= 0)
            {
              (ItemsSource as List<DisassemblyLine>)[line_index].IsBreakpoint = false;
              m_vertical_scroll_bar.RemoveAnnotation(line_index, false, Brushes.Red);
            }
          }
          break;
      }

    }


    #region · INotifyPropertyChanged interface ·

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      if (PropertyChanged != null && propertyName != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}