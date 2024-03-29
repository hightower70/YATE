﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using YATE.Disassembler;
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


    public bool GoToAddresPopupOpened { get; set; } = false;

    public bool AddBreakpointPopupOpened { get; set; } = false;

    public ICommand ToggleBreakpointCommand { get; private set; }
    public ICommand LoadAssemblerListCommand { get; private set; }
    public ICommand GotoAddressCommand { get; private set; }
    public ICommand AddBreakpointCommand { get; private set; }

    public DisassemblyListView(IndexedWindowManager in_window_manager)
    {
      m_tstate_min = 0;
      m_tstate_max = 0;
      m_tstate_sum_string = "0/0";

      m_window_manager = in_window_manager;
      m_window_index = m_window_manager.AcquireWindowIndex();

      m_settings = SettingsFile.Default.GetSettings<DisassemblyListViewSettings>(m_window_index);

      ToggleBreakpointCommand = new DisassemblyListViewCommand(OnToggleBreakpoint);
      LoadAssemblerListCommand = new DisassemblyListViewCommand(OnLoadAssemblerList);
      GotoAddressCommand = new DisassemblyListViewCommand(OnGoToAddress);
      AddBreakpointCommand = new DisassemblyListViewCommand(OnAddBreakpoint);

      DataContext = this;

      InitializeComponent();

      (TVCManagers.Default.ExecutionManager as ExecutionManager).DebuggerEvent += DebuggerEventHandler;

      tmsMemorySelector.SetSelector(m_settings.MemorySelection);

      tmsMemorySelector.SelectionChanged = MemorySelectionChanged;
      dlbDisassembly.MemoryType = m_settings.MemorySelection.MemoryType;

      UpdateDisassemblyList();
    }
    private void wDisassemblyList_ContentRendered(object sender, EventArgs e)
    {
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

    private void OnToggleBreakpoint(object parameter)
    {
      object selected_item = dlbDisassembly.SelectedItem;

      if (selected_item == null)
        return;

      DisassemblyLine line = selected_item as DisassemblyLine;

      if (line.IsBreakpoint)
        RemoveBreakpoint(line);
      else
        AddBreakpoint(line);
    }

    private void AddBreakpoint(DisassemblyLine in_line)
    {
      BreakpointInfo breakpoint = new BreakpointInfo(m_settings.MemorySelection.MemoryType, (ushort)in_line.DisassemblyInstruction.Address, (ushort)m_settings.MemorySelection.PageIndex);

      ((BreakpointManager)TVCManagers.Default.BreakpointManager).AddBreakpoint(breakpoint);

    }

    private void RemoveBreakpoint(DisassemblyLine in_line)
    {
      BreakpointInfo breakpoint = new BreakpointInfo(m_settings.MemorySelection.MemoryType, (ushort)in_line.DisassemblyInstruction.Address, (ushort)m_settings.MemorySelection.PageIndex);

      ((BreakpointManager)TVCManagers.Default.BreakpointManager).RemoveBreakpoint(breakpoint);
    }

    private void DebuggerEventHandler(TVComputer in_sender, DebugEventType in_event_type)
    {
      switch (in_event_type)
      {
        case DebugEventType.Paused:
          {
            ExecutionManager execution_manager = (ExecutionManager)TVCManagers.Default.ExecutionManager;
            DebugManager debug_manager = (DebugManager)TVCManagers.Default.DebugManager;
            TVCMemory tvc_memory = (TVCMemory)execution_manager.TVC.Memory;
            ushort address = execution_manager.TVC.CPU.Registers.PC;
            TVCMemoryType memory_type = tvc_memory.GetMemoryTypeAtAddress(address);

            if (memory_type == m_memory.MemoryType)
            {
              IDebuggableMemory memory = debug_manager.GetDebuggableMemory(memory_type);
              if (memory.PageIndex == m_memory.PageIndex)
              {
                int index = GetItemIndexAtAddress(address);

                if (index >= 0)
                {
                  if (m_current_line != null)
                    m_current_line.IsExecuting = false;

                  SelectItemAtAddress(in_sender.CPU.Registers.PC);

                  m_current_line = (DisassemblyLine)dlbDisassembly.SelectedItem;
                  if (m_current_line != null)
                    m_current_line.IsExecuting = true;
                }
              }
            }
          }
          break;

        case DebugEventType.Running:
          if (m_current_line != null)
            m_current_line.IsExecuting = false;
          m_current_line = null;
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
        MemoryDisassembler disassembler = new MemoryDisassembler();
        disassembler.MemoryStartAddress = (ushort)tmsMemorySelector.StartAddress;
        disassembler.MemoryEndAddress = (ushort)tmsMemorySelector.EndAddress;
        disassembler.MemoryPageIndex = m_settings.MemorySelection.PageIndex;
        disassembler.MemoryType = m_settings.MemorySelection.MemoryType;

        m_disassembly_collection = disassembler.Disassemble();
        m_current_line = null;

        dlbDisassembly.ItemsSource = m_disassembly_collection;
      }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      (TVCManagers.Default.ExecutionManager as ExecutionManager).DebuggerEvent -= DebuggerEventHandler;

      SettingsFile.Default.SetSettings(m_settings, m_window_index);
      SettingsFile.Default.Save();

      m_window_manager.ReleaseWindowIndex(m_window_index);
    }

    private void SelectItemAtAddress(ushort in_address)
    {
      int index = GetItemIndexAtAddress(in_address);

      if (index >= 0)
      {
        dlbDisassembly.SelectedIndex = index;
        dlbDisassembly.ScrollToCenterOfView(dlbDisassembly.SelectedItem, false);
      }
    }

    private void SelectItemAtAddressOrBefore(ushort in_address)
    {
      int index = GetItemIndexAtAddressOrBefore(in_address);

      if (index >= 0)
      {
        dlbDisassembly.SelectedIndex = index;
        dlbDisassembly.ScrollToCenterOfView(dlbDisassembly.SelectedItem, false);
      }
    }

    private int GetItemIndexAtAddress(ushort in_address)
    {
      DisassemblyLine search = new DisassemblyLine();
      search.DisassemblyInstruction.Address = in_address;

      int index = m_disassembly_collection.IndexOf(search);

      return index;
    }

    private int GetItemIndexAtAddressOrBefore(ushort in_address)
    {
      int index;

      index = -1;
      while (index + 1 < m_disassembly_collection.Count && m_disassembly_collection[index + 1].DisassemblyInstruction.Address <= in_address)
      {
        index++;
      }

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

      // add execution position
      if (execution_manager.ExecutionState == ExecutionManager.ExecutionStates.Paused)
      {
        TVCMemory tvc_memory = (TVCMemory)execution_manager.TVC.Memory;
        ushort address = execution_manager.TVC.CPU.Registers.PC;
        TVCMemoryType memory_type = tvc_memory.GetMemoryTypeAtAddress(address);

        if (memory_type == m_memory.MemoryType)
        {
          IDebuggableMemory memory = debug_manager.GetDebuggableMemory(memory_type);
          if (memory.PageIndex == m_memory.PageIndex)
          {
            int index = GetItemIndexAtAddress(address);

            if (index >= 0)
            {
              m_current_line = m_disassembly_collection[index];
              if (m_current_line != null)
                m_current_line.IsExecuting = true;
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

    private void OnLoadAssemblerList(object parameter)
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

        AssemblerListReader reader = new AssemblerListReader();
        reader.RegisterParser(new SJASMListParser());
        reader.ReadAssemblyList(filename);

        m_disassembly_collection = reader.Disassembly;
        dlbDisassembly.ItemsSource = m_disassembly_collection;

        UpdateBreakpoints();
      }

    }

    private void OnGoToAddress(object parameter)
    {
      GoToAddresPopupOpened = true;
      OnPropertyChanged("GoToAddresPopupOpened");
      tbGotoAddress.Focus();
    }

    private void OnAddBreakpoint(object parameter)
    {
      AddBreakpointPopupOpened = true;
      OnPropertyChanged("AddBreakpointPopupOpened");
      tbBreakpointAddress.Focus();
    }

    private void tbGotoAddress_LostFocus(object sender, RoutedEventArgs e)
    {
      GoToAddresPopupOpened = false;
      OnPropertyChanged("GoToAddresPopupOpened");
    }

    private void tbGotoAddress_KeyDown(object sender, KeyEventArgs e)
    {
      switch(e.Key)
      {
        case Key.Escape:
          CloseGoToAddressPopup();
          break;

        case Key.Enter:
          {
            IntegerExpressionEvaluator integer_expression_evaluator = new IntegerExpressionEvaluator();

            try
            {
              int address = integer_expression_evaluator.ParseAndEvaluate(tbGotoAddress.Text);

              SelectItemAtAddressOrBefore((ushort)address);
            }
            catch
            {

            }

            CloseGoToAddressPopup();
          }
          break;
      }
    }

    private void CloseGoToAddressPopup()
    {
      GoToAddresPopupOpened = false;
      OnPropertyChanged("GoToAddresPopupOpened");
    }

    private void GotoAddressCloseButton_Click(object sender, RoutedEventArgs e)
    {
      CloseGoToAddressPopup();
    }

    private void tbBreakpointAddress_LostFocus(object sender, RoutedEventArgs e)
    {
      AddBreakpointPopupOpened = false;
      OnPropertyChanged("AddBreakpointPopupOpened");
    }

    private void tbBreakpointAddress_KeyDown(object sender, KeyEventArgs e)
    {
      switch (e.Key)
      {
        case Key.Escape:
          CloseAddBreakpointPopup();
          break;

        case Key.Enter:
          {
            IntegerExpressionEvaluator integer_expression_evaluator = new IntegerExpressionEvaluator();

            try
            {
              int address = integer_expression_evaluator.ParseAndEvaluate(tbBreakpointAddress.Text);

              SelectItemAtAddressOrBefore((ushort)address);

              if(dlbDisassembly.SelectedItem != null) 
              {
                AddBreakpoint((DisassemblyLine)dlbDisassembly.SelectedItem);
              }
            }
            catch
            {

            }

            CloseAddBreakpointPopup();
          }
          break;
      }
    }

    private void BreakpointAddressCloseButton_Click(object sender, RoutedEventArgs e)
    {
      CloseAddBreakpointPopup();
    }

    private void CloseAddBreakpointPopup()
    {
      AddBreakpointPopupOpened = false;
      OnPropertyChanged("AddBreakpointPopupOpened");
    }
  }
}
