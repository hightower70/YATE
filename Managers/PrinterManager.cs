///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2023 Laszlo Arvai. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
// MA 02110-1301  USA
///////////////////////////////////////////////////////////////////////////////
// File description
// ----------------
// Printer Capture Manager, UI controller
///////////////////////////////////////////////////////////////////////////////
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using YATE.Forms;
using YATE.Settings;
using YATECommon;
using YATECommon.Settings;
using System.Windows;

namespace YATE.Managers
{
  public class PrinterManager : IPrinterManager, INotifyPropertyChanged
  {
    private ExecutionManager m_execution_manager;
    private MainWindow m_main_window;
    private string m_disable_print_menu_item_text;

    /// <summary>
    /// Printer capture close menu item text to bind menuitem
    /// </summary>
    public string DisableMenuItemText
    {
      get { return m_disable_print_menu_item_text; }
      set { m_disable_print_menu_item_text = value; OnPropertyChanged("DisableMenuItemText"); }
    }

    /// <summary>
    /// Initializes Printer Manager class
    /// </summary>
    /// <param name="in_main_window">Main window reference</param>
    /// <param name="in_execution_control">Execution manager reference</param>
    public void Initialize(MainWindow in_main_window, ExecutionManager in_execution_control)
    {
      m_main_window = in_main_window;
      m_execution_manager = in_execution_control;

      // Get cartridge file folder
      PrinterSettings settings = SettingsFile.Default.GetSettings<PrinterSettings>();
      if(settings.IsPrinterCaptureEnabled && !string.IsNullOrEmpty(settings.PrinterCaptureFileName))
      {
        SetPrinterDisableMenuItemText(settings.PrinterCaptureFileName);
        m_execution_manager.TVC.Printer.PrinterFileName = settings.PrinterCaptureFileName;
      }
      else
      {
        SetPrinterDisableMenuItemText(string.Empty);
        m_execution_manager.TVC.Printer.PrinterFileName = string.Empty;
      }
    }

    /// <summary>
    /// Enabled printer capture
    /// </summary>
    public void OnPrinterCaptureEnabled()
    {
      Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
      {
        DefaultExt = ".txt",
        Filter = "Text file (*.txt)|*.TXT|All files (*.*)|*.*"
      };

      // Get cartridge file folder
      PrinterSettings settings = SettingsFile.Default.GetSettings<PrinterSettings>();

      if (settings != null && !string.IsNullOrEmpty(settings.PrinterCaptureFileName))
      {
        dlg.InitialDirectory = Path.GetDirectoryName(settings.PrinterCaptureFileName);
        dlg.FileName = Path.GetFileName(settings.PrinterCaptureFileName);
      }

      // Show open file dialog box
      bool? result = null;
      result = dlg.ShowDialog();

      // Process open file dialog box results
      if (result == true)
      {
        string file_name = dlg.FileName;

        m_execution_manager.TVC.Printer.PrinterFileName = file_name;

        SetPrinterDisableMenuItemText(file_name);

        // save cartidge file name
        settings.IsPrinterCaptureEnabled = true;
        settings.PrinterCaptureFileName = file_name;
        SettingsFile.Default.SetSettings(settings);
        SettingsFile.Default.Save();
      }
    }

    /// <summary>
    /// Disables printer capture
    /// </summary>
    public void OnPrinterCaptureDisabled()
    {
      m_execution_manager.TVC.Printer.PrinterFileName = null;

      PrinterSettings settings = SettingsFile.Default.GetSettings<PrinterSettings>();

      settings.IsPrinterCaptureEnabled = true;
      SettingsFile.Default.SetSettings(settings);
      SettingsFile.Default.Save();
    }

    /// <summary>
    /// Sets 'Printer Close' menu item text
    /// </summary>
    /// <param name="in_printer_file_name"></param>
    private void SetPrinterDisableMenuItemText(string in_printer_file_name)
    {
      if (string.IsNullOrEmpty(in_printer_file_name))
        DisableMenuItemText = (string)Application.Current.FindResource("srClosePrinterMenu");
      else
        DisableMenuItemText = string.Format((string)Application.Current.FindResource("srClosePrinterMenuParam"), Path.GetFileName(in_printer_file_name));
    }

    #region · INotifyPropertyChanged Members ·

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
