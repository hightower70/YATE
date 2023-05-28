using CustomControls;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using YATECommon;

namespace HBF.Forms
{
  /// <summary>
  /// Interaction logic for DriveDiskSettings.xaml
  /// </summary>
  public partial class SetupDiskDrive : UserControl
  {
    public SetupDiskDrive()
    {
      InitializeComponent();
    }

    private void bImageFileBrowser_Click(object sender, RoutedEventArgs e)
    {
      HBFDriveSettings settings = DataContext as HBFDriveSettings;

      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
      {
        Filter = "Disk Image File (*.dsk)|*.dsk|(*.img)|*.img|Binary file (*.bin)|*.bin|All Files (*.*)|*.*"
      };

      if (!string.IsNullOrEmpty((DataContext as HBFDriveSettings).DiskImageFile))
      {
        dlg.InitialDirectory = Path.GetDirectoryName(settings.DiskImageFile);
      }

      // Show open file dialog box
      bool? result = null;
      result = dlg.ShowDialog();

      // Process open file dialog box results
      if (result == true)
      {
        settings.DiskImageFile = dlg.FileName;
      }
    }


    private void bUPMFoderBrowse_Click(object sender, RoutedEventArgs e)
    {
      HBFDriveSettings settings = DataContext as HBFDriveSettings;

      FolderBrowserDialog dialog = new FolderBrowserDialog()
      {
        InitialDirectory = settings.UPMFolder
      };

      if (dialog.ShowDialog(TVCManagers.Default.MainWindow) == true)
      {
        settings.UPMFolder = dialog.FileName;
      }
    }

    private void bFATFolderBrowser_Click(object sender, RoutedEventArgs e)
    {
      HBFDriveSettings settings = DataContext as HBFDriveSettings;

      FolderBrowserDialog dialog = new FolderBrowserDialog()
      {
        InitialDirectory = settings.FATFolder
      };

      if (dialog.ShowDialog(TVCManagers.Default.MainWindow) == true)
      {
        settings.FATFolder = dialog.FileName;
      }
    }
  }
}
