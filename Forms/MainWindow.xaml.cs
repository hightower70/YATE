using CustomControls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YATE.Controls;
using YATE.Dialogs;
using YATE.Drivers;
using YATE.Emulator.TVCFiles;
using YATE.Emulator.TVCHardware;
using YATE.Settings;
using YATECommon.ExpansionManager;
using YATECommon.Settings;

namespace YATE.Forms
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
	{
		private WriteableBitmap m_image_source = null;
		private Int32Rect m_refresh_rect;

		public ExecutionControl ExecutionControl { get; }
		public TVCCartridgeControl CartridgeControl { get; }

		private KeyboardHook m_keyboard_hook = new KeyboardHook();

		public MainWindow()
		{
			// load config file and get settings
			SettingsFile.Default.Load();

			m_keyboard_hook.EnableHook(Window_KeyDown);

			// setup execution control
			ExecutionControl = new ExecutionControl(this);

			ExecutionControl.Initialize();
			ExecutionControl.TVC.Video.FrameReady += FrameReady;

			// setup cartridge control
			CartridgeControl = new TVCCartridgeControl();
			CartridgeControl.Initialize(this, ExecutionControl);

			// setup data context for data binding
			DataContext = this;

			// load modules
			ExpansionManager.Default.AddMainModule(typeof(MainModule));
      ExpansionManager.Default.LoadExpansions();
      ExpansionManager.Default.InstallExpansions(ExecutionControl.TVC);

      InitializeComponent();

			ExecutionControl.Start();
		}

		private void FrameReady(object sender, TVCVideo.FrameReadyEventparam in_event_param)
		{
			// reallocate bitmap if needed
			if (m_image_source == null || in_event_param.Width != m_image_source.Width || in_event_param.Height != m_image_source.Height)
			{
				m_refresh_rect = new Int32Rect(0, 0, in_event_param.Width, in_event_param.Height);
				m_image_source = new WriteableBitmap(in_event_param.Width, in_event_param.Height, 96, 96, PixelFormats.Bgr32, null);
				iDisplay.Width = in_event_param.Width;
				iDisplay.Height = in_event_param.Height;
				iDisplay.Source = m_image_source;
			}

			int stride = in_event_param.Width * (System.Drawing.Image.GetPixelFormatSize(System.Drawing.Imaging.PixelFormat.Format32bppRgb) / 8);

			m_image_source.WritePixels(m_refresh_rect, in_event_param.FrameData, stride, 0);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			ExecutionControl.Stop();
			m_keyboard_hook.ReleaseHook();

			MainGeneralSettings settings = SettingsFile.Default.GetSettings<MainGeneralSettings>();

			settings.MainWindowPos.SaveWindowPositionAndSize(this);

			SettingsFile.Default.SetSettings(settings);
			SettingsFile.Default.Save();
		}

		private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			ExecutionControl.TVC.Keyboard.KeyDown(e);
		}

		private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			ExecutionControl.TVC.Keyboard.KeyUp(e);
		}

		private void MiOpenCASFile_Click(object sender, RoutedEventArgs e)
		{
			// Configure open file dialog box
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
			{
				DefaultExt = ".cas",
				Filter = "All supported files (*.cas; *.zip; *.dsk)|*.CAS;*.ZIP;*.DSK|All files (*.*)|*.*"
			};

			// Show open file dialog box
			bool? result = null;
			result = dlg.ShowDialog();

			// Process open file dialog box results
			if (result == true)
			{
				// Open document
				string filename = dlg.FileName;

				TVCFiles.LoadProgramFile(filename, ExecutionControl.TVC.Memory);
			}
		}

		private void MiSaveAsCASFile_Click(object sender, RoutedEventArgs e)
		{
			// Configure open file dialog box
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
			{
				DefaultExt = ".cas",
				Filter = "Program file (*.cas)|*.CAS|Basic text file [ANSI encoded] (*.bas)|*.bas|Basic text file [UTF-8 encoded] (*.bas)|*.bas|Basic text file [Unicode encoded] (*.bas)|*.bas"
			};

			// Show open file dialog box
			bool? result = null;
			result = dlg.ShowDialog();

			// Process open file dialog box results
			if (result == true)
			{
				// Open document
				string filename = dlg.FileName;

				switch(dlg.FilterIndex)
				{
					case 1:
						TVCFiles.SaveProgramFile(filename, ExecutionControl.TVC.Memory);
						break;

					case 2:
						TVCFiles.SaveProgramFile(filename, ExecutionControl.TVC.Memory, BASFile.EncodingType.Ansi);
						break;

					case 3:
						TVCFiles.SaveProgramFile(filename, ExecutionControl.TVC.Memory, BASFile.EncodingType.Utf8);
						break;

					case 4:
						TVCFiles.SaveProgramFile(filename, ExecutionControl.TVC.Memory, BASFile.EncodingType.Unicode);
						break;
				}
			}
		}

		private void MiCartridgeMemoryLoad_Click(object sender, RoutedEventArgs e)
		{
			CartridgeControl.OnCartridgeMemoryLoad();
		}

		private void MiCartridgeMemoryClear_Click(object sender, RoutedEventArgs e)
		{
			CartridgeControl.OnCartridgeMemoryClear();
		}

		private void MiLoadFromGameBase_Click(object sender, RoutedEventArgs e)
		{
			GameBaseBrowser dialog = new GameBaseBrowser();

			dialog.Owner = this;
			if( dialog.ShowDialog() == true)
			{
				string filename = dialog.SelectedFileName;

        // load program 
				TVCFiles.LoadProgramFile(filename, ExecutionControl.TVC.Memory);

        // autostart program is enabled
        GamebaseSettings settings = SettingsFile.Default.GetSettings<GamebaseSettings>();
        if (settings.Autostart)
        {
          ExecutionControl.TVC.Keyboard.InjectKeys("DR,W,UR,DU,W,UU,DN,W,UN,DEnter,W,UEnter");
        }
			}
		}

		private void miOptions_Click(object sender, RoutedEventArgs e)
		{
			Window setup = new SetupDialog();
			setup.Owner = this;
			if (setup.ShowDialog() ?? false)
			{						
				using (new WaitCursor())
				{
					// stop modules and dispatcher timer

					// save settings if dialog result was success
					SettingsFile.Default.CopySettingsFrom(SettingsFile.Editing);
					SettingsFile.Default.Save();

					// reload modules
          //TODO:javitani
					//ExpansionManager.Default.LoadExpansions();
          //ExpansionManager.Default.InstallExpansions(ExecutionControl.TVC);
        }
      }
		}
	}
}
