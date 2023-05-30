using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YATECommon
{
  public interface IPrinterManager
  {
    void OnPrinterCaptureEnabled();
    void OnPrinterCaptureDisabled();
  }
}
