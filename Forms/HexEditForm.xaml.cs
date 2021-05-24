using CustomControls.HexEdit;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using YATE.Emulator.TVCHardware;
using YATECommon;

namespace YATE.Forms
{
  /// <summary>
  /// Interaction logic for HexEditForm.xaml
  /// </summary>
  public partial class HexEditForm : Window, INotifyPropertyChanged
  {
    private BinaryReader binaryReader;

    public event PropertyChangedEventHandler PropertyChanged;

    public HexEditForm()
    {
      InitializeComponent();
      /*
  // Generate random data so we display something right out of the box without forcing the user to open a file
  var rand = new Random();

  // 10 MB of random data
  var bytes = new byte[64 * 1024];
  rand.NextBytes(bytes);

  Reader = new BinaryReader(new ByteArrayStream(bytes));              
  */

      Reader = new BinaryReader(new ByteArrayStream(((TVComputer)TVCManagers.Default.ExecutionManager.ITVC).Memory.DebugUserMemory));
    }

    /// <summary>
    /// Gets or sets the source of the data to display using the <see cref="HexViewer"/> control.
    /// </summary>
    public BinaryReader Reader
    {
      get => binaryReader;

      set
      {
        binaryReader = value;

        OnPropertyChanged();
      }
    }


    private void OnPropertyChanged([CallerMemberName] string name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

  }
}
