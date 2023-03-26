using CustomControls.HexEdit;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using YATE.Emulator.TVCHardware;
using YATE.Managers;
using YATE.Settings;
using YATECommon;
using YATECommon.Settings;
using static YATE.Controls.TVCMemorySelector;

namespace YATE.Forms
{

  internal class HexEditDataProvider : IHexEditorDataProvider
  {
    private IDebuggableMemory m_memory;
    private int m_page_index;

    public HexEditDataProvider(IDebuggableMemory in_memory, int in_page_index)
    {
      m_memory = in_memory;
      m_page_index = in_page_index;
    }

    public long Length { get => m_memory.MemorySize; }

    public long Position { get ; set; }

    public byte ReadByte()
    {
      return m_memory.DebugReadMemory(m_page_index, (int)Position++);
    }
  }


  /// <summary>
  /// Interaction logic for HexEditForm.xaml
  /// </summary>
  public partial class HexEditForm : Window, INotifyPropertyChanged
  {
    private HexViewSettings m_settings;
    private IndexedWindowManager m_window_manager;
    private int m_window_index;

    public event PropertyChangedEventHandler PropertyChanged;

    public HexEditForm(IndexedWindowManager in_window_manager)
    {
      m_window_manager = in_window_manager;

      InitializeComponent();

      m_window_index = m_window_manager.AcquireWindowIndex();
      m_settings = SettingsFile.Default.GetSettings<HexViewSettings>(m_window_index);

      (TVCManagers.Default.ExecutionManager as ExecutionManager).DebuggerStoppedEvent += DebuggerStoppedEventHandler;

      tmsMemorySelector.SetSelector(m_settings.MemorySelection);
      CreateDataProvider();

      tmsMemorySelector.SelectionChanged = MemorySelectionChanged;
    }

    private void OnPropertyChanged([CallerMemberName] string name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      (TVCManagers.Default.ExecutionManager as ExecutionManager).DebuggerStoppedEvent -= DebuggerStoppedEventHandler;

      SettingsFile.Default.SetSettings(m_settings, m_window_index);
      SettingsFile.Default.Save();

      m_window_manager.ReleaseWindowIndex(m_window_index);
    }

    private void DebuggerStoppedEventHandler(TVComputer in_sender)
    {
      heEditor.Refresh();
    }

    private void MemorySelectionChanged(object sender, MemorySelectionChangedEventType in_selection_changed_type)
    {
      CreateDataProvider();
    }

    private void CreateDataProvider()
    { 
      IDebuggableMemory memory = TVCManagers.Default.DebugManager.GetDebuggableMemory(m_settings.MemorySelection.MemoryType);

      if (memory == null)
        heEditor.DataProvider = null;
      else
      {
        IHexEditorDataProvider data_provider = new HexEditDataProvider(memory, m_settings.MemorySelection.PageIndex);

        heEditor.DataProvider = data_provider;
        heEditor.AddressOffset = (ulong)memory.AddressOffset;
      }
    }
  }
}
