using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVCEmuCommon.Settings
{
  public class SlotSelectionDataProvider : INotifyPropertyChanged
  {
    private SettingsFile m_settings_file;

    public ObservableCollection<string> Slots { get; private set; }

    public SlotSelectionDataProvider(SettingsFile in_settings_file, CardBaseSettings in_card_settings)
    {
      m_settings_file = in_settings_file;
      Slots = new ObservableCollection<string>();
      UpdateSlotList();
    }

    public void UpdateSlotList()
    {
      IList<ModuleBaseSettingsInfo> slots = m_settings_file.GetSlotList();

      for (int slot_index = 0; slot_index < slots.Count; slot_index++)
      {
        string slot_string = string.Format("Slot #{0} - {1}", slot_index, (slots[slot_index] == null) ? "Empty" : slots[slot_index].ModuleName);
        if (Slots.Count <= slot_index)
          Slots.Add(slot_string);
        else
          Slots[slot_index] = slot_string;
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
