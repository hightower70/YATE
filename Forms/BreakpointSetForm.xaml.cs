using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace YATE.Forms
{
  /// <summary>
  /// Interaction logic for BreakpointSetForm.xaml
  /// </summary>
  public partial class BreakpointSetForm : Window
  {
    public int BreakpointAddress;

    public BreakpointSetForm()
    {
      InitializeComponent();
    }

    private void BSet_Click(object sender, RoutedEventArgs e)
    {
      int breakpoint_address;

      if (int.TryParse(tbAddress.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out breakpoint_address))
      {
        BreakpointAddress = breakpoint_address;
      }
      else
      {
        tbAddress.Text = "";
        BreakpointAddress = - 1;
      }

      DialogResult = true;
    }

    private void BClear_Click(object sender, RoutedEventArgs e)
    {
      BreakpointAddress = - 1;

      DialogResult = true;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      if (BreakpointAddress != -1)
        tbAddress.Text = BreakpointAddress.ToString("X4");
    }
  }
}
