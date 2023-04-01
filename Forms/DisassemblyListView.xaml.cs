using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YATE.Controls;
using YATE.Emulator.Files;
using YATE.Emulator.TVCHardware;
using YATE.Emulator.Z80CPU;
using YATE.Managers;
using YATE.Settings;
using YATECommon;
using YATECommon.Helpers;
using YATECommon.Settings;
using static YATE.Controls.TVCMemorySelector;

namespace YATE.Forms
{
  /// <summary>
  /// Interaction logic for DisassemblyListView.xaml
  /// </summary>
  public partial class DisassemblyListView : Window, INotifyPropertyChanged
  {
    public class DisassemblyListViewCommand : ICommand
    {
      public delegate void ICommandOnExecute(object parameter);

      private ICommandOnExecute m_execute;
      private bool m_can_execute;

      public DisassemblyListViewCommand(ICommandOnExecute onExecuteMethod)
      {
        m_execute = onExecuteMethod;
        m_can_execute = true;
      }

      #region ICommand Members

      public event EventHandler CanExecuteChanged;

      public bool CanExecute(object parameter)
      {
        return m_can_execute;
      }

      public void SetCanExecute(bool in_can_execute)
      {
        if (m_can_execute != in_can_execute)
        {
          m_can_execute = in_can_execute;
          CanExecuteChanged.Invoke(this, EventArgs.Empty);
        }
      }

      public void Execute(object parameter)
      {
        m_execute.Invoke(parameter);
      }

      #endregion
    }

    private IDebuggableMemory m_memory;
    private List<DisassemblyLine> m_disassembly_collection;
    private DisassemblyListViewSettings m_settings;
    private IndexedWindowManager m_window_manager;
    private int m_window_index;
    private DisassemblyLine m_current_line;

    private int m_tstate_min;
    private int m_tstate_max;
    private string m_tstate_sum_string;

    public ExecutionManager ExecutionControl { get { return (ExecutionManager)TVCManagers.Default.ExecutionManager; } }

    public ICommand ToggleBreakpointCommand { get; private set; }

    public DisassemblyListView(IndexedWindowManager in_window_manager)
    {
      m_tstate_min = 0;
      m_tstate_max = 0;
      m_tstate_sum_string = "0/0";

      m_window_manager = in_window_manager;
      m_window_index = m_window_manager.AcquireWindowIndex();

      m_settings = SettingsFile.Default.GetSettings<DisassemblyListViewSettings>(m_window_index);

      ToggleBreakpointCommand = new DisassemblyListViewCommand(ToggleBreakpoint);

      DataContext = this;

      InitializeComponent();

      (TVCManagers.Default.ExecutionManager as ExecutionManager).DebuggerEvent += DebuggerEventHandler;

      tmsMemorySelector.SetSelector(m_settings.MemorySelection);

      tmsMemorySelector.SelectionChanged = MemorySelectionChanged;
      dlbDisassembly.MemoryType = m_settings.MemorySelection.MemoryType;

      UpdateDisassemblyList();
      UpdateBreakpoints();
    }

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

    private void ToggleBreakpoint(object parameter)
    {
      object selected_item = dlbDisassembly.SelectedItem;

      if (selected_item == null)
        return;

      DisassemblyLine line = selected_item as DisassemblyLine;
      BreakpointInfo breakpoint = new BreakpointInfo(m_settings.MemorySelection.MemoryType, line.DisassemblyInstruction.Address, (ushort)m_settings.MemorySelection.PageIndex);


      if (line.IsBreakpoint)
        ((BreakpointManager)TVCManagers.Default.BreakpointManager).RemoveBreakpoint(breakpoint);
      else
        ((BreakpointManager)TVCManagers.Default.BreakpointManager).AddBreakpoint(breakpoint);
    }

    private void DebuggerEventHandler(TVComputer in_sender, DebugEventType in_event_type)
    {
      int index;

      switch (in_event_type)
      {
        case DebugEventType.Paused:
          if (m_current_line != null)
            m_current_line.IsExecuting = false;

          SelectItemAtAddress(in_sender.CPU.Registers.PC);

          m_current_line = (DisassemblyLine)dlbDisassembly.SelectedItem;
          if (m_current_line != null)
            m_current_line.IsExecuting = true;
          break;

        case DebugEventType.Running:
          index = GetItemIndexAtAddress(in_sender.CPU.Registers.PC);
          if (index >= 0)
          {
            m_disassembly_collection[index].IsExecuting = false;
          }
          break;
      }
    }

    private void MemorySelectionChanged(object sender, MemorySelectionChangedEventType in_selection_changed_type)
    {
      dlbDisassembly.Focus();
      UpdateDisassemblyList();
    }

    private void UpdateDisassemblyList()
    {
      TVCMemoryType memory_type = m_settings.MemorySelection.MemoryType;
      m_memory = TVCManagers.Default.DebugManager.GetDebuggableMemory(memory_type);
      dlbDisassembly.MemoryType = memory_type;

      if (m_memory == null)
      {
        dlbDisassembly.ItemsSource = null;
      }
      else
      {
        m_disassembly_collection = new List<DisassemblyLine>();

        Disassemble();

        dlbDisassembly.ItemsSource = m_disassembly_collection;
      }
    }

    private void Disassemble()
    {
      DisassemblyLine disassembly_line;
      m_current_line = null;

      Z80Disassembler disassembler = new Z80Disassembler();
      disassembler.ReadByte = ReadMemoryByte;
      disassembler.HexConstDisplayMode = Z80Disassembler.HexConstDisplay.Postfix;

      int address = tmsMemorySelector.StartAddress;
      int end_address = tmsMemorySelector.EndAddress;

      IDebuggableMemory memory = TVCManagers.Default.DebugManager.GetDebuggableMemory(m_settings.MemorySelection.MemoryType);
      if (memory != null)
      {
        if (address < memory.AddressOffset)
          address = memory.AddressOffset;

        if (end_address < address)
          end_address = address;
        if (end_address > memory.AddressOffset + memory.MemorySize)
          end_address = memory.AddressOffset + memory.MemorySize;

        disassembler.AddressOffset = memory.AddressOffset;

        // disassembly memory
        while (address < end_address)
        {
          Z80DisassemblerInstruction current_instruction = disassembler.Disassemble((ushort)address);
          disassembly_line = new DisassemblyLine(current_instruction);
          disassembly_line.Index = m_disassembly_collection.Count;

          m_disassembly_collection.Add(disassembly_line);
          address += current_instruction.Length;
        }
      }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      (TVCManagers.Default.ExecutionManager as ExecutionManager).DebuggerEvent -= DebuggerEventHandler;

      SettingsFile.Default.SetSettings(m_settings, m_window_index);
      SettingsFile.Default.Save();

      m_window_manager.ReleaseWindowIndex(m_window_index);
    }

    private byte ReadMemoryByte(ushort in_address)
    {
      return m_memory.DebugReadMemory(m_settings.MemorySelection.PageIndex, in_address);
    }

    private void SelectItemAtAddress(ushort in_address)
    {
      int index = GetItemIndexAtAddress(in_address);

      if (index >= 0)
      {
        dlbDisassembly.SelectedIndex = index;
        dlbDisassembly.ScrollToCenterOfView(dlbDisassembly.SelectedItem);
      }
    }

    private int GetItemIndexAtAddress(ushort in_address)
    {
      DisassemblyLine search = new DisassemblyLine();
      search.DisassemblyInstruction.Address = in_address;

      int index = m_disassembly_collection.BinarySearch(search);

      return index;
    }

    private void dlbDisassembly_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
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

    private void UpdateBreakpoints()
    {
      ExecutionManager execution_manager = (ExecutionManager)TVCManagers.Default.ExecutionManager;
      BreakpointManager breakpoint_manager = (BreakpointManager)TVCManagers.Default.BreakpointManager;
      DebugManager debug_manager = (DebugManager)TVCManagers.Default.DebugManager;

      // copy breakpoints
      foreach (BreakpointInfo breakpoint in breakpoint_manager.Breakpoints)
      {
        if (breakpoint.MemoryType == m_memory.MemoryType && breakpoint.Page == m_memory.PageIndex)
        {
          int index = GetItemIndexAtAddress(breakpoint.Address);

          if (index >= 0)
          {
            dlbDisassembly.AddBreakpointToLine(index);
          }
        }
      }

      // add execaution position
      if(execution_manager.ExecutionState == ExecutionManager.ExecutionStates.Paused)
      {
        TVCMemory tvc_memory = (TVCMemory)execution_manager.TVC.Memory;
        ushort address = execution_manager.TVC.CPU.Registers.PC;
        TVCMemoryType memory_type = tvc_memory.GetMemoryTypeAtAddress(address);

        if (memory_type == m_memory.MemoryType)
        {
          IDebuggableMemory memory = debug_manager.GetDebuggableMemory(memory_type);
          if(memory.PageIndex == m_memory.PageIndex)
          {
            int index = GetItemIndexAtAddress(address);

            if(index>=0)
            {
              m_disassembly_collection[index].IsExecuting = true;
            }
          }
        }
      }
    }

    #region · PropertyChanged members ·

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      if (PropertyChanged != null && propertyName != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    private void btnLoadList_Click(object sender, RoutedEventArgs e)
    {
      // Configure open file dialog box
      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
      {
        DefaultExt = ".lst",
        Filter = "Assembler List Files (*.lst)|*.LST|All files (*.*)|*.*"
      };

      // Show open file dialog box
      bool? result = dlg.ShowDialog();

      // Process open file dialog box results
      if (result == true)
      {
        // Open document
        string filename = dlg.FileName;

        
      }

    }
  }
}
