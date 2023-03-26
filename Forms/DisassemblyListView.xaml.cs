using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using YATE.Controls;
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

    public DisassemblyListView(IndexedWindowManager in_window_manager)
    {
      m_tstate_min = 0;
      m_tstate_max = 0;
      m_tstate_sum_string = "0/0";

      m_window_manager = in_window_manager;
      m_window_index = m_window_manager.AcquireWindowIndex();

      m_settings = SettingsFile.Default.GetSettings<DisassemblyListViewSettings>(m_window_index);

      DataContext = this;

      InitializeComponent();

      (TVCManagers.Default.ExecutionManager as ExecutionManager).DebuggerStoppedEvent += DebuggerStoppedEventHandler;

      tmsMemorySelector.SetSelector(m_settings.MemorySelection);

      tmsMemorySelector.SelectionChanged = MemorySelectionChanged;

      UpdateDisassemblyList();
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

    private void DebuggerStoppedEventHandler(TVComputer in_sender)
    {
      if (m_current_line != null)
        m_current_line.IsExecuting = false;

      SelectItemAtAddress(in_sender.CPU.Registers.PC);

      m_current_line = (DisassemblyLine)dlbDisassembly.SelectedItem;
      if (m_current_line != null)
        m_current_line.IsExecuting = true;
    }

    private void MemorySelectionChanged(object sender, MemorySelectionChangedEventType in_selection_changed_type)
    {
      UpdateDisassemblyList();
    }

    private void UpdateDisassemblyList()
    {
      m_memory = TVCManagers.Default.DebugManager.GetDebuggableMemory(m_settings.MemorySelection.MemoryType);

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

      ushort address = (ushort)tmsMemorySelector.StartAddress;
      ushort end_address = (ushort)tmsMemorySelector.EndAddress;

      IDebuggableMemory memory = TVCManagers.Default.DebugManager.GetDebuggableMemory(m_settings.MemorySelection.MemoryType);
      if (memory != null)
      {
        if (address < memory.AddressOffset)
          address = (ushort)memory.AddressOffset;

        if (end_address < address)
          end_address = address;
        if (end_address > memory.AddressOffset + memory.MemorySize)
          end_address = (ushort)(memory.AddressOffset + memory.MemorySize);
      }

      while (address < end_address)
      {
        Z80DisassemblerInstruction current_instruction = disassembler.Disassemble(address);
        disassembly_line = new DisassemblyLine(current_instruction);

        m_disassembly_collection.Add(disassembly_line);
        address += current_instruction.Length;
      }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      (TVCManagers.Default.ExecutionManager as ExecutionManager).DebuggerStoppedEvent -= DebuggerStoppedEventHandler;

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
      DisassemblyLine search = new DisassemblyLine();
      search.DisassemblyInstruction.Address = in_address;

      int index = m_disassembly_collection.BinarySearch(search);

      if (index >= 0)
      {
        dlbDisassembly.SelectedIndex = index;
        dlbDisassembly.ScrollToCenterOfView(dlbDisassembly.SelectedItem);
      }
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

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      if (PropertyChanged != null && propertyName != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
