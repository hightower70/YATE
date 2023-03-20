using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using YATE.Settings;
using YATECommon;
using YATECommon.Helpers;

namespace YATE.Controls
{
  /// <summary>
  /// Interaction logic for TVCMemorySelector.xaml
  /// </summary>
  public partial class TVCMemorySelector : UserControl, INotifyPropertyChanged
  {
    public enum MemorySelectionChangedEventType
    {
      MemoryType,
      MemoryPage,
      StartAddress,
      EndAddress
    };

    public delegate void MemorySelectionChangedDelegate(object sender, MemorySelectionChangedEventType in_selection_changed_type);

    public MemorySelectionChangedDelegate SelectionChanged { get; set; }

    private IntegerExpressionEvaluator m_expression_evaluator = new IntegerExpressionEvaluator();
    private TVCMemorySelectorSettings m_memory_selector_settings;

    private int m_start_address;
    private int m_end_address;

    public int StartAddress
    {
      get { return m_start_address; }
      set { m_start_address = value; OnPropertyChanged(); }
    }

    public int EndAddress
    {
      get { return m_end_address; }
      set { m_end_address = value; OnPropertyChanged(); }
    }

    public TVCMemorySelector()
    {
      InitializeComponent();

      elStartAddress.EditToLabelConverter = StartAddressEvaluator;
      elEndAddress.EditToLabelConverter = EndAddressEvaluator;
    }

    public void SetSelector(TVCMemorySelectorSettings in_selector)
    {
      m_memory_selector_settings = in_selector;

      cbMemoryType.SelectedIndex = (int)m_memory_selector_settings.MemoryType;
      cbPage.SelectedIndex = m_memory_selector_settings.PageIndex;
      elStartAddress.Text = m_memory_selector_settings.StartAddress;
      elEndAddress.Text = m_memory_selector_settings.EndAddress;
    }

    private string StartAddressEvaluator(string in_text)
    {
      string result;

      try
      {
        StartAddress = m_expression_evaluator.ParseAndEvaluate(in_text);
        result = StartAddress.ToString("X4") + "h";
      }
      catch (Exception ex)
      {
        StartAddress = -1;
        result = ex.Message;
      }

      OnSelectionChanged(MemorySelectionChangedEventType.StartAddress);

      return result;
    }

    private string EndAddressEvaluator(string in_text)
    {
      string result;

      try
      {
        EndAddress = m_expression_evaluator.ParseAndEvaluate(in_text);
        result = EndAddress.ToString("X4") + "h";
      }
      catch (Exception ex)
      {
        EndAddress = -1;
        result = ex.Message;
      }

      OnSelectionChanged(MemorySelectionChangedEventType.EndAddress);

      return result;
    }

    #region · INotifyPropertyHandler ·

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      if (PropertyChanged != null && propertyName != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    private void MemoryType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_memory_selector_settings.MemoryType = (TVCMemoryType)cbMemoryType.SelectedIndex;

      IDebuggableMemory memory = TVCManagers.Default.DebugManager.GetDebuggableMemory(m_memory_selector_settings.MemoryType);

      bool pages_visible = (memory != null && memory.PageCount > 1);

      Visibility page_visibility = (pages_visible) ? Visibility.Visible : Visibility.Collapsed;

      tbPage.Visibility = page_visibility;
      cbPage.Visibility = page_visibility;

      if(pages_visible)
      {
        string[] page_names = memory.PageNames;
        cbPage.ItemsSource = page_names;
        if (m_memory_selector_settings.PageIndex < page_names.Length)
        {
          cbPage.SelectedIndex = m_memory_selector_settings.PageIndex;
        }
        else
        {
          if (page_names.Length > 0)
          {
            cbPage.SelectedIndex = page_names.Length - 1;
            m_memory_selector_settings.PageIndex = page_names.Length - 1;
          }
        }
      }

      OnSelectionChanged(MemorySelectionChangedEventType.MemoryType);
    }
    
    private void MemoryPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_memory_selector_settings.PageIndex = cbPage.SelectedIndex;

      OnSelectionChanged(MemorySelectionChangedEventType.MemoryPage);
    }

    private void OnSelectionChanged(MemorySelectionChangedEventType in_event_type)
    {
      if(SelectionChanged != null)
      {
        SelectionChanged(this, in_event_type);
      }
    }

  }
}

