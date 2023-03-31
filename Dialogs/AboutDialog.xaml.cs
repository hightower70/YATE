using System;
using System.Deployment.Application;
using System.Reflection;
using System.Windows;

namespace YATE.Dialogs
{
  /// <summary>
  /// Interaction logic for AboutDialog.xaml
  /// </summary>
  public partial class AboutDialog : Window

  {
    public string Version { get; }

    public AboutDialog()
    {
      Version = GetRunningVersion();

      InitializeComponent();
      DataContext = this;
    }

    private string GetRunningVersion()
    {
      Version version;

      try
      {
        version = ApplicationDeployment.CurrentDeployment.CurrentVersion;
      }
      catch (Exception)
      {
        version = Assembly.GetExecutingAssembly().GetName().Version;
      }

      return String.Format("{0}.{1}.{2}",version.Major, version.Minor, version.Build);
    }
  }
}
