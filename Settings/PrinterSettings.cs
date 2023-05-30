using YATECommon.Settings;

namespace YATE.Settings
{
  internal class PrinterSettings : SettingsBase
  {
    public string PrinterCaptureFileName { get; set; }
    public bool IsPrinterCaptureEnabled { get; set; }

    public PrinterSettings() : base(SettingsCategory.Emulator, "PrinterCapture")
    {
      SetDefaultValues();
    }

    override public void SetDefaultValues()
    {
      PrinterCaptureFileName = "";
      IsPrinterCaptureEnabled = false;
    }
  }
}
