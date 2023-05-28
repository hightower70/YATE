using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HBF
{
  public class HBFDriveSettings : INotifyPropertyChanged
  {
    private int m_emulation_mode;
    private string m_disk_image_file;
    private string m_upm_folder;
    private string m_fat_folder;

    public static string[] DiskEmulationModes { get; } = new string[] { "No drive", "Disk Image File", "Virtual UPM Disk", "Virtual VTDOS Disk" };

    /// <summary>
    /// Binary disk image 
    /// </summary>
    public string DiskImageFile { get { return m_disk_image_file; } set { m_disk_image_file = value; OnPropertyChanged(); } }
    
    /// <summary>
    /// UPM disk files folder
    /// </summary>
    public string UPMFolder { get { return m_upm_folder; } set { m_upm_folder = value; OnPropertyChanged(); } }

    /// <summary>
    /// FAT disk files folders
    /// </summary>
    public string FATFolder { get { return m_fat_folder; } set { m_fat_folder = value; OnPropertyChanged(); } }

    /// <summary>
    /// Disk emulation mode setting
    /// </summary>
    public int EmulationMode { get { return m_emulation_mode; } set { m_emulation_mode = value; OnPropertyChanged(); } }

    /// <summary>
    /// Sets default values
    /// </summary>
    public void SetDefaultValues()
    {
      m_emulation_mode = 0;

      DiskImageFile = "";
      UPMFolder = "";
      FATFolder = "";
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
